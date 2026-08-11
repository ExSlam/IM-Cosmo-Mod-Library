using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Schema-specific JSON codec for the lightweight IM Data Core sidecar.
    ///
    /// This deliberately does not use UnityEngine.JsonUtility. The sidecar is
    /// private IMDC data, and relying on Unity's serializer allowed supported
    /// scalar fields to be written while the record collections were silently
    /// omitted in the actual Idol Manager runtime.
    ///
    /// The codec always writes Checkpoints, Events, and CustomMutations, even
    /// when they are empty. Deserialization rejects a document that omits any of
    /// those required collections so a header-only/truncated sidecar can never
    /// be accepted as a valid empty history.
    /// </summary>
    internal static class LightweightSidecarJson
    {
        internal static string Serialize(LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            if (document.Checkpoints == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar Checkpoints collection is null.");
            }

            if (document.Events == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar Events collection is null.");
            }

            if (document.CustomMutations == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar CustomMutations collection is null.");
            }

            StringBuilder builder = new StringBuilder(
                Math.Max(256, EstimateCapacity(document)));

            builder.Append('{');

            AppendPropertyName(builder, "FormatName");
            AppendString(builder, document.FormatName ?? string.Empty);
            builder.Append(',');

            AppendPropertyName(builder, "FormatVersion");
            AppendInt32(builder, document.FormatVersion);
            builder.Append(',');

            AppendPropertyName(builder, "RelativeSavePath");
            AppendString(builder, document.RelativeSavePath ?? string.Empty);
            builder.Append(',');

            AppendPropertyName(builder, "LastIssuedSequence");
            AppendInt64(builder, document.LastIssuedSequence);
            builder.Append(',');

            AppendPropertyName(builder, "Checkpoints");
            AppendCheckpoints(builder, document.Checkpoints);
            builder.Append(',');

            AppendPropertyName(builder, "Events");
            AppendEvents(builder, document.Events);
            builder.Append(',');

            AppendPropertyName(builder, "CustomMutations");
            AppendCustomMutations(builder, document.CustomMutations);

            builder.Append('}');
            return builder.ToString();
        }

        internal static LightweightSidecarDocument Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("The lightweight sidecar JSON is empty.");
            }

            JsonValue rootValue = new JsonParser(json).ParseDocument();
            Dictionary<string, JsonValue> root = RequireObject(
                rootValue,
                "The lightweight sidecar root must be a JSON object.");

            LightweightSidecarDocument document = new LightweightSidecarDocument
            {
                FormatName = RequireString(root, "FormatName"),
                FormatVersion = RequireInt32(root, "FormatVersion"),
                RelativeSavePath = RequireString(root, "RelativeSavePath"),
                LastIssuedSequence = RequireInt64(root, "LastIssuedSequence"),
                Checkpoints = ReadCheckpoints(RequireArray(root, "Checkpoints")),
                Events = ReadEvents(RequireArray(root, "Events")),
                CustomMutations = ReadCustomMutations(
                    RequireArray(root, "CustomMutations"))
            };

            return document;
        }

        private static int EstimateCapacity(LightweightSidecarDocument document)
        {
            long estimate = 192L;
            estimate += (long)document.Checkpoints.Count * 160L;
            estimate += (long)document.Events.Count * 320L;
            estimate += (long)document.CustomMutations.Count * 240L;

            if (estimate > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)estimate;
        }

        private static void AppendCheckpoints(
            StringBuilder builder,
            List<LightweightCheckpointRecord> records)
        {
            builder.Append('[');
            for (int index = 0; index < records.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                LightweightCheckpointRecord record = records[index];
                if (record == null)
                {
                    throw new InvalidOperationException(
                        "The lightweight sidecar contains a null checkpoint record.");
                }

                builder.Append('{');

                AppendPropertyName(builder, "RelativeSavePath");
                AppendString(builder, record.RelativeSavePath ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "LastSave");
                AppendString(builder, record.LastSave ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "PlaytimeSeconds");
                AppendInt64(builder, record.PlaytimeSeconds);
                builder.Append(',');

                AppendPropertyName(builder, "GameDateTime");
                AppendString(builder, record.GameDateTime ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "Sequence");
                AppendInt64(builder, record.Sequence);

                builder.Append('}');
            }

            builder.Append(']');
        }

        private static void AppendEvents(
            StringBuilder builder,
            List<LightweightEventRecord> records)
        {
            builder.Append('[');
            for (int index = 0; index < records.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                LightweightEventRecord record = records[index];
                if (record == null)
                {
                    throw new InvalidOperationException(
                        "The lightweight sidecar contains a null event record.");
                }

                builder.Append('{');

                AppendPropertyName(builder, "Sequence");
                AppendInt64(builder, record.Sequence);
                builder.Append(',');

                AppendPropertyName(builder, "EventId");
                AppendInt64(builder, record.EventId);
                builder.Append(',');

                AppendPropertyName(builder, "GameDateKey");
                AppendInt32(builder, record.GameDateKey);
                builder.Append(',');

                AppendPropertyName(builder, "GameDateTime");
                AppendString(builder, record.GameDateTime ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "IdolId");
                AppendInt32(builder, record.IdolId);
                builder.Append(',');

                AppendPropertyName(builder, "EntityKind");
                AppendString(builder, record.EntityKind ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "EntityId");
                AppendString(builder, record.EntityId ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "EventType");
                AppendString(builder, record.EventType ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "SourcePatch");
                AppendString(builder, record.SourcePatch ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "NamespaceIdentifier");
                AppendString(builder, record.NamespaceIdentifier ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "PayloadJson");
                AppendString(builder, record.PayloadJson ?? string.Empty);

                builder.Append('}');
            }

            builder.Append(']');
        }

        private static void AppendCustomMutations(
            StringBuilder builder,
            List<LightweightCustomMutationRecord> records)
        {
            builder.Append('[');
            for (int index = 0; index < records.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                LightweightCustomMutationRecord record = records[index];
                if (record == null)
                {
                    throw new InvalidOperationException(
                        "The lightweight sidecar contains a null custom mutation.");
                }

                builder.Append('{');

                AppendPropertyName(builder, "Sequence");
                AppendInt64(builder, record.Sequence);
                builder.Append(',');

                AppendPropertyName(builder, "GameDateKey");
                AppendInt32(builder, record.GameDateKey);
                builder.Append(',');

                AppendPropertyName(builder, "GameDateTime");
                AppendString(builder, record.GameDateTime ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "NamespaceIdentifier");
                AppendString(builder, record.NamespaceIdentifier ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "DataKey");
                AppendString(builder, record.DataKey ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "Operation");
                AppendString(builder, record.Operation ?? string.Empty);
                builder.Append(',');

                AppendPropertyName(builder, "ValueJson");
                AppendString(builder, record.ValueJson ?? string.Empty);

                builder.Append('}');
            }

            builder.Append(']');
        }

        private static List<LightweightCheckpointRecord> ReadCheckpoints(
            List<JsonValue> values)
        {
            List<LightweightCheckpointRecord> records =
                new List<LightweightCheckpointRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "A checkpoint entry must be a JSON object.");

                records.Add(
                    new LightweightCheckpointRecord
                    {
                        RelativeSavePath = RequireString(
                            item,
                            "RelativeSavePath"),
                        LastSave = RequireString(item, "LastSave"),
                        PlaytimeSeconds = RequireInt64(
                            item,
                            "PlaytimeSeconds"),
                        GameDateTime = RequireString(
                            item,
                            "GameDateTime"),
                        Sequence = RequireInt64(item, "Sequence")
                    });
            }

            return records;
        }

        private static List<LightweightEventRecord> ReadEvents(
            List<JsonValue> values)
        {
            List<LightweightEventRecord> records =
                new List<LightweightEventRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "An event entry must be a JSON object.");

                records.Add(
                    new LightweightEventRecord
                    {
                        Sequence = RequireInt64(item, "Sequence"),
                        EventId = RequireInt64(item, "EventId"),
                        GameDateKey = RequireInt32(item, "GameDateKey"),
                        GameDateTime = RequireString(item, "GameDateTime"),
                        IdolId = RequireInt32(item, "IdolId"),
                        EntityKind = RequireString(item, "EntityKind"),
                        EntityId = RequireString(item, "EntityId"),
                        EventType = RequireString(item, "EventType"),
                        SourcePatch = RequireString(item, "SourcePatch"),
                        NamespaceIdentifier = RequireString(
                            item,
                            "NamespaceIdentifier"),
                        PayloadJson = RequireString(item, "PayloadJson")
                    });
            }

            return records;
        }

        private static List<LightweightCustomMutationRecord> ReadCustomMutations(
            List<JsonValue> values)
        {
            List<LightweightCustomMutationRecord> records =
                new List<LightweightCustomMutationRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "A custom mutation entry must be a JSON object.");

                records.Add(
                    new LightweightCustomMutationRecord
                    {
                        Sequence = RequireInt64(item, "Sequence"),
                        GameDateKey = RequireInt32(item, "GameDateKey"),
                        GameDateTime = RequireString(item, "GameDateTime"),
                        NamespaceIdentifier = RequireString(
                            item,
                            "NamespaceIdentifier"),
                        DataKey = RequireString(item, "DataKey"),
                        Operation = RequireString(item, "Operation"),
                        ValueJson = RequireString(item, "ValueJson")
                    });
            }

            return records;
        }

        private static Dictionary<string, JsonValue> RequireObject(
            JsonValue value,
            string errorMessage)
        {
            if (value == null || value.Kind != JsonValueKind.Object)
            {
                throw new FormatException(errorMessage);
            }

            return value.ObjectValue;
        }

        private static List<JsonValue> RequireArray(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value = RequireMember(source, name);
            if (value.Kind != JsonValueKind.Array)
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' must be a JSON array.");
            }

            return value.ArrayValue;
        }

        private static string RequireString(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value = RequireMember(source, name);
            if (value.Kind != JsonValueKind.String)
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' must be a JSON string.");
            }

            return value.StringValue ?? string.Empty;
        }

        private static int RequireInt32(
            Dictionary<string, JsonValue> source,
            string name)
        {
            long value = RequireInt64(source, name);
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' is outside the Int32 range.");
            }

            return (int)value;
        }

        private static long RequireInt64(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value = RequireMember(source, name);
            long result;
            if (value.Kind != JsonValueKind.Number ||
                !long.TryParse(
                    value.NumberValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' must be a JSON integer.");
            }

            return result;
        }

        private static JsonValue RequireMember(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value;
            if (!source.TryGetValue(name, out value) || value == null)
            {
                throw new FormatException(
                    "The lightweight sidecar is missing required field '" +
                    name + "'.");
            }

            return value;
        }

        private static void AppendPropertyName(
            StringBuilder builder,
            string name)
        {
            AppendString(builder, name);
            builder.Append(':');
        }

        private static void AppendInt32(StringBuilder builder, int value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendInt64(StringBuilder builder, long value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendString(
            StringBuilder builder,
            string value)
        {
            builder.Append('"');

            if (!string.IsNullOrEmpty(value))
            {
                for (int index = 0; index < value.Length; index++)
                {
                    char character = value[index];
                    switch (character)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            if (character < ' ')
                            {
                                builder.Append("\\u");
                                builder.Append(
                                    ((int)character).ToString(
                                        "x4",
                                        CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                builder.Append(character);
                            }

                            break;
                    }
                }
            }

            builder.Append('"');
        }

        private enum JsonValueKind
        {
            Null,
            Object,
            Array,
            String,
            Number,
            Boolean
        }

        private sealed class JsonValue
        {
            internal JsonValueKind Kind;
            internal Dictionary<string, JsonValue> ObjectValue;
            internal List<JsonValue> ArrayValue;
            internal string StringValue;
            internal string NumberValue;
            internal bool BooleanValue;
        }

        private sealed class JsonParser
        {
            private readonly string json;
            private int index;

            internal JsonParser(string json)
            {
                this.json = json ?? string.Empty;
            }

            internal JsonValue ParseDocument()
            {
                SkipWhitespace();
                JsonValue value = ParseValue();
                SkipWhitespace();

                if (index != json.Length)
                {
                    throw Error("Unexpected trailing JSON content.");
                }

                return value;
            }

            private JsonValue ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length)
                {
                    throw Error("Unexpected end of JSON input.");
                }

                char character = json[index];
                switch (character)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return new JsonValue
                        {
                            Kind = JsonValueKind.String,
                            StringValue = ParseString()
                        };
                    case 't':
                        ReadLiteral("true");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Boolean,
                            BooleanValue = true
                        };
                    case 'f':
                        ReadLiteral("false");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Boolean,
                            BooleanValue = false
                        };
                    case 'n':
                        ReadLiteral("null");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Null
                        };
                    default:
                        if (character == '-' ||
                            (character >= '0' && character <= '9'))
                        {
                            return new JsonValue
                            {
                                Kind = JsonValueKind.Number,
                                NumberValue = ParseNumber()
                            };
                        }

                        throw Error("Unexpected JSON token.");
                }
            }

            private JsonValue ParseObject()
            {
                Expect('{');
                SkipWhitespace();

                Dictionary<string, JsonValue> values =
                    new Dictionary<string, JsonValue>(StringComparer.Ordinal);

                if (TryConsume('}'))
                {
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Object,
                        ObjectValue = values
                    };
                }

                while (true)
                {
                    SkipWhitespace();
                    if (index >= json.Length || json[index] != '"')
                    {
                        throw Error("Expected a JSON object property name.");
                    }

                    string name = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    JsonValue value = ParseValue();

                    if (values.ContainsKey(name))
                    {
                        throw Error(
                            "Duplicate JSON object property '" + name + "'.");
                    }

                    values.Add(name, value);
                    SkipWhitespace();

                    if (TryConsume('}'))
                    {
                        break;
                    }

                    Expect(',');
                }

                return new JsonValue
                {
                    Kind = JsonValueKind.Object,
                    ObjectValue = values
                };
            }

            private JsonValue ParseArray()
            {
                Expect('[');
                SkipWhitespace();

                List<JsonValue> values = new List<JsonValue>();
                if (TryConsume(']'))
                {
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Array,
                        ArrayValue = values
                    };
                }

                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();

                    if (TryConsume(']'))
                    {
                        break;
                    }

                    Expect(',');
                }

                return new JsonValue
                {
                    Kind = JsonValueKind.Array,
                    ArrayValue = values
                };
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder builder = new StringBuilder();

                while (index < json.Length)
                {
                    char character = json[index++];
                    if (character == '"')
                    {
                        return builder.ToString();
                    }

                    if (character == '\\')
                    {
                        if (index >= json.Length)
                        {
                            throw Error("Unterminated JSON escape sequence.");
                        }

                        char escaped = json[index++];
                        switch (escaped)
                        {
                            case '"':
                                builder.Append('"');
                                break;
                            case '\\':
                                builder.Append('\\');
                                break;
                            case '/':
                                builder.Append('/');
                                break;
                            case 'b':
                                builder.Append('\b');
                                break;
                            case 'f':
                                builder.Append('\f');
                                break;
                            case 'n':
                                builder.Append('\n');
                                break;
                            case 'r':
                                builder.Append('\r');
                                break;
                            case 't':
                                builder.Append('\t');
                                break;
                            case 'u':
                                builder.Append(ParseUnicodeEscape());
                                break;
                            default:
                                throw Error("Invalid JSON escape sequence.");
                        }

                        continue;
                    }

                    if (character < ' ')
                    {
                        throw Error(
                            "Unescaped control character in JSON string.");
                    }

                    builder.Append(character);
                }

                throw Error("Unterminated JSON string.");
            }

            private char ParseUnicodeEscape()
            {
                if (index + 4 > json.Length)
                {
                    throw Error("Incomplete JSON unicode escape.");
                }

                int value = 0;
                for (int offset = 0; offset < 4; offset++)
                {
                    char character = json[index++];
                    value <<= 4;

                    if (character >= '0' && character <= '9')
                    {
                        value += character - '0';
                    }
                    else if (character >= 'a' && character <= 'f')
                    {
                        value += character - 'a' + 10;
                    }
                    else if (character >= 'A' && character <= 'F')
                    {
                        value += character - 'A' + 10;
                    }
                    else
                    {
                        throw Error("Invalid JSON unicode escape.");
                    }
                }

                return (char)value;
            }

            private string ParseNumber()
            {
                int start = index;

                if (json[index] == '-')
                {
                    index++;
                    if (index >= json.Length)
                    {
                        throw Error("Incomplete JSON number.");
                    }
                }

                if (json[index] == '0')
                {
                    index++;
                }
                else
                {
                    if (json[index] < '1' || json[index] > '9')
                    {
                        throw Error("Invalid JSON number.");
                    }

                    while (index < json.Length &&
                           json[index] >= '0' &&
                           json[index] <= '9')
                    {
                        index++;
                    }
                }

                if (index < json.Length && json[index] == '.')
                {
                    index++;
                    int fractionStart = index;
                    while (index < json.Length &&
                           json[index] >= '0' &&
                           json[index] <= '9')
                    {
                        index++;
                    }

                    if (fractionStart == index)
                    {
                        throw Error("Invalid JSON number fraction.");
                    }
                }

                if (index < json.Length &&
                    (json[index] == 'e' || json[index] == 'E'))
                {
                    index++;
                    if (index < json.Length &&
                        (json[index] == '+' || json[index] == '-'))
                    {
                        index++;
                    }

                    int exponentStart = index;
                    while (index < json.Length &&
                           json[index] >= '0' &&
                           json[index] <= '9')
                    {
                        index++;
                    }

                    if (exponentStart == index)
                    {
                        throw Error("Invalid JSON number exponent.");
                    }
                }

                return json.Substring(start, index - start);
            }

            private void ReadLiteral(string literal)
            {
                if (index + literal.Length > json.Length ||
                    string.CompareOrdinal(
                        json,
                        index,
                        literal,
                        0,
                        literal.Length) != 0)
                {
                    throw Error("Invalid JSON literal.");
                }

                index += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length)
                {
                    char character = json[index];
                    if (character == ' ' ||
                        character == '\t' ||
                        character == '\r' ||
                        character == '\n')
                    {
                        index++;
                        continue;
                    }

                    break;
                }
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (index >= json.Length || json[index] != expected)
                {
                    throw Error(
                        "Expected JSON character '" + expected + "'.");
                }

                index++;
            }

            private FormatException Error(string message)
            {
                return new FormatException(
                    message + " Offset: " +
                    index.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }
    }
}
