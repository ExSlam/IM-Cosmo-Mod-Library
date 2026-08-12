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

        internal static string CompactMoneyTransactionPayload(
            string payloadJson,
            out bool changed)
        {
            changed = false;
            if (string.IsNullOrEmpty(payloadJson))
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue outerValue = new JsonParser(payloadJson).ParseDocument();
                if (outerValue == null || outerValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                JsonValue detailMember;
                if (!outerValue.ObjectValue.TryGetValue(
                        "detail_json",
                        out detailMember) ||
                    detailMember == null ||
                    detailMember.Kind != JsonValueKind.String ||
                    string.IsNullOrEmpty(detailMember.StringValue))
                {
                    return payloadJson;
                }

                JsonValue detailValue =
                    new JsonParser(detailMember.StringValue).ParseDocument();
                if (detailValue == null || detailValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                List<string> keysToRemove = new List<string>();
                foreach (KeyValuePair<string, JsonValue> pair
                    in detailValue.ObjectValue)
                {
                    if (string.Equals(
                            pair.Key,
                            "kind",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (IsMoneyDetailDefault(pair.Key, pair.Value))
                    {
                        keysToRemove.Add(pair.Key);
                    }
                }

                if (keysToRemove.Count == 0)
                {
                    return payloadJson;
                }

                for (int index = 0; index < keysToRemove.Count; index++)
                {
                    detailValue.ObjectValue.Remove(keysToRemove[index]);
                }

                outerValue.ObjectValue["detail_json"] = new JsonValue
                {
                    Kind = JsonValueKind.String,
                    StringValue = SerializeJsonValue(detailValue)
                };

                changed = true;
                return SerializeJsonValue(outerValue);
            }
            catch
            {
                // Payload compaction is an optimization. Preserve the original
                // capture verbatim if a future payload shape is not understood.
                changed = false;
                return payloadJson;
            }
        }

        internal static bool TryReadCsvIntProperty(
            string json,
            string propertyName,
            int minimumValue,
            out List<int> values)
        {
            values = new List<int>();
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                JsonValue root = new JsonParser(json).ParseDocument();
                if (root == null || root.Kind != JsonValueKind.Object)
                {
                    return false;
                }

                JsonValue property;
                if (!root.ObjectValue.TryGetValue(propertyName, out property) ||
                    property == null ||
                    property.Kind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(property.StringValue))
                {
                    return false;
                }

                HashSet<int> uniqueValues = new HashSet<int>();
                string[] tokens = property.StringValue.Split(',');
                for (int index = 0; index < tokens.Length; index++)
                {
                    int parsed;
                    if (int.TryParse(
                            tokens[index].Trim(),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out parsed) &&
                        parsed >= minimumValue &&
                        uniqueValues.Add(parsed))
                    {
                        values.Add(parsed);
                    }
                }

                return values.Count > 0;
            }
            catch
            {
                values.Clear();
                return false;
            }
        }

        internal static bool TryReadIntProperty(
            string json,
            string propertyName,
            out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                JsonValue root = new JsonParser(json).ParseDocument();
                JsonValue property;
                long parsed;
                if (root == null ||
                    root.Kind != JsonValueKind.Object ||
                    !root.ObjectValue.TryGetValue(propertyName, out property) ||
                    property == null ||
                    property.Kind != JsonValueKind.Number ||
                    !long.TryParse(
                        property.NumberValue,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsed) ||
                    parsed < int.MinValue ||
                    parsed > int.MaxValue)
                {
                    return false;
                }

                value = (int)parsed;
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        internal static bool TryReadStringProperty(
            string json,
            string propertyName,
            out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                JsonValue root = new JsonParser(json).ParseDocument();
                JsonValue property;
                if (root == null ||
                    root.Kind != JsonValueKind.Object ||
                    !root.ObjectValue.TryGetValue(propertyName, out property) ||
                    property == null ||
                    property.Kind != JsonValueKind.String)
                {
                    return false;
                }

                value = property.StringValue ?? string.Empty;
                return true;
            }
            catch
            {
                value = string.Empty;
                return false;
            }
        }

        internal static string ExpandMoneyTransactionPayloadForPublic(
            string payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson))
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue outerValue = new JsonParser(payloadJson).ParseDocument();
                if (outerValue == null || outerValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                JsonValue detailMember;
                if (!outerValue.ObjectValue.TryGetValue(
                        "detail_json",
                        out detailMember) ||
                    detailMember == null ||
                    detailMember.Kind != JsonValueKind.String ||
                    string.IsNullOrEmpty(detailMember.StringValue))
                {
                    return payloadJson;
                }

                JsonValue detailValue =
                    new JsonParser(detailMember.StringValue).ParseDocument();
                if (detailValue == null || detailValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                AddMoneyDetailDefaults(detailValue.ObjectValue);
                outerValue.ObjectValue["detail_json"] = new JsonValue
                {
                    Kind = JsonValueKind.String,
                    StringValue = SerializeJsonValue(detailValue)
                };
                return SerializeJsonValue(outerValue);
            }
            catch
            {
                return payloadJson;
            }
        }

        private static bool IsMoneyDetailDefault(
            string fieldName,
            JsonValue value)
        {
            if (string.IsNullOrEmpty(fieldName) || value == null)
            {
                return false;
            }

            MoneyDetailDefaultKind defaultKind;
            if (!TryGetMoneyDetailDefaultKind(fieldName, out defaultKind))
            {
                // Future fields are retained until 2.0.5 explicitly knows their
                // declared default. This prevents accidental lossy compaction.
                return false;
            }

            switch (defaultKind)
            {
                case MoneyDetailDefaultKind.EmptyString:
                    return value.Kind == JsonValueKind.String &&
                        string.IsNullOrEmpty(value.StringValue);

                case MoneyDetailDefaultKind.Zero:
                    double number;
                    return value.Kind == JsonValueKind.Number &&
                        double.TryParse(
                            value.NumberValue,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out number) &&
                        number == 0d;

                case MoneyDetailDefaultKind.MinusOne:
                    long integer;
                    return value.Kind == JsonValueKind.Number &&
                        long.TryParse(
                            value.NumberValue,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out integer) &&
                        integer == -1L;

                case MoneyDetailDefaultKind.False:
                    return value.Kind == JsonValueKind.Boolean &&
                        !value.BooleanValue;

                case MoneyDetailDefaultKind.EmptyArray:
                    return value.Kind == JsonValueKind.Array &&
                        (value.ArrayValue == null || value.ArrayValue.Count == 0);

                default:
                    return false;
            }
        }

        private static void AddMoneyDetailDefaults(
            Dictionary<string, JsonValue> fields)
        {
            if (fields == null)
            {
                return;
            }

            string[] fieldNames = MoneyDetailFieldNames;
            for (int index = 0; index < fieldNames.Length; index++)
            {
                string fieldName = fieldNames[index];
                if (fields.ContainsKey(fieldName))
                {
                    continue;
                }

                MoneyDetailDefaultKind defaultKind;
                if (TryGetMoneyDetailDefaultKind(fieldName, out defaultKind))
                {
                    fields.Add(fieldName, CreateMoneyDetailDefault(defaultKind));
                }
            }
        }

        private static JsonValue CreateMoneyDetailDefault(
            MoneyDetailDefaultKind defaultKind)
        {
            switch (defaultKind)
            {
                case MoneyDetailDefaultKind.EmptyString:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.String,
                        StringValue = string.Empty
                    };

                case MoneyDetailDefaultKind.MinusOne:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Number,
                        NumberValue = "-1"
                    };

                case MoneyDetailDefaultKind.False:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Boolean,
                        BooleanValue = false
                    };

                case MoneyDetailDefaultKind.EmptyArray:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Array,
                        ArrayValue = new List<JsonValue>()
                    };

                default:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Number,
                        NumberValue = "0"
                    };
            }
        }

        private static bool TryGetMoneyDetailDefaultKind(
            string fieldName,
            out MoneyDetailDefaultKind defaultKind)
        {
            defaultKind = MoneyDetailDefaultKind.Zero;

            switch (fieldName)
            {
                case "contract_type_code":
                case "contractor_name":
                case "product_name":
                case "idol_name":
                case "single_title":
                case "single_group_name":
                case "single_genre_token":
                case "single_lyrics_token":
                case "single_choreography_token":
                case "show_title":
                case "show_medium_token":
                case "show_genre_token":
                case "show_host_token":
                case "staff_name":
                case "staff_role_code":
                case "theater_title":
                case "theater_income_type":
                case "theater_performance_type":
                case "theater_audience_type":
                case "cafe_title":
                case "cafe_dish_title":
                case "cafe_dish_type":
                case "cafe_appeal_type":
                case "concert_title":
                case "concert_venue":
                    defaultKind = MoneyDetailDefaultKind.EmptyString;
                    return true;

                case "idol_id":
                case "theater_id":
                case "cafe_id":
                case "concert_id":
                    defaultKind = MoneyDetailDefaultKind.MinusOne;
                    return true;

                case "single_marketing_tokens":
                case "participant_names":
                case "cafe_staff_names":
                    defaultKind = MoneyDetailDefaultKind.EmptyArray;
                    return true;

                case "has_fan_audience":
                case "concert_finished":
                    defaultKind = MoneyDetailDefaultKind.False;
                    return true;

                case "payment_amount":
                case "stamina_cost":
                case "liability_amount":
                case "multiplier":
                case "negotiations":
                case "gross_revenue":
                case "production_cost":
                case "show_episode_number":
                case "show_audience":
                case "show_fan_audience":
                case "show_fatigue":
                case "show_weekly_budget":
                case "salary_amount":
                case "idol_fame":
                case "idol_scandal_points":
                case "theater_ticket_price":
                case "theater_attendance":
                case "theater_subscription_price":
                case "theater_subscriber_delta":
                case "theater_subscriber_total":
                case "cafe_new_fans":
                case "concert_ticket_price":
                case "concert_projected_attendance":
                case "concert_projected_hype":
                case "concert_finished_hype":
                case "concert_finished_revenue":
                case "concert_finished_profit":
                case "concert_accident_count":
                case "concert_accident_successes":
                case "concert_accident_failures":
                case "concert_accident_critical_failures":
                    defaultKind = MoneyDetailDefaultKind.Zero;
                    return true;

                default:
                    return false;
            }
        }

        private static readonly string[] MoneyDetailFieldNames =
        {
            "contract_type_code",
            "contractor_name",
            "product_name",
            "payment_amount",
            "stamina_cost",
            "liability_amount",
            "idol_id",
            "idol_name",
            "multiplier",
            "negotiations",
            "single_title",
            "single_group_name",
            "single_genre_token",
            "single_lyrics_token",
            "single_choreography_token",
            "single_marketing_tokens",
            "participant_names",
            "gross_revenue",
            "production_cost",
            "show_title",
            "show_medium_token",
            "show_genre_token",
            "show_host_token",
            "show_episode_number",
            "show_audience",
            "has_fan_audience",
            "show_fan_audience",
            "show_fatigue",
            "show_weekly_budget",
            "staff_name",
            "staff_role_code",
            "salary_amount",
            "idol_fame",
            "idol_scandal_points",
            "theater_id",
            "theater_title",
            "theater_income_type",
            "theater_ticket_price",
            "theater_performance_type",
            "theater_audience_type",
            "theater_attendance",
            "theater_subscription_price",
            "theater_subscriber_delta",
            "theater_subscriber_total",
            "cafe_id",
            "cafe_title",
            "cafe_dish_title",
            "cafe_dish_type",
            "cafe_staff_names",
            "cafe_new_fans",
            "cafe_appeal_type",
            "concert_id",
            "concert_title",
            "concert_venue",
            "concert_ticket_price",
            "concert_projected_attendance",
            "concert_projected_hype",
            "concert_finished_hype",
            "concert_finished",
            "concert_finished_revenue",
            "concert_finished_profit",
            "concert_accident_count",
            "concert_accident_successes",
            "concert_accident_failures",
            "concert_accident_critical_failures"
        };

        private enum MoneyDetailDefaultKind
        {
            EmptyString,
            Zero,
            MinusOne,
            False,
            EmptyArray
        }

        private static string SerializeJsonValue(JsonValue value)
        {
            StringBuilder builder = new StringBuilder(256);
            AppendJsonValue(builder, value);
            return builder.ToString();
        }

        private static void AppendJsonValue(
            StringBuilder builder,
            JsonValue value)
        {
            if (value == null || value.Kind == JsonValueKind.Null)
            {
                builder.Append("null");
                return;
            }

            if (value.Kind == JsonValueKind.Object)
            {
                builder.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, JsonValue> pair
                    in value.ObjectValue)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    AppendString(builder, pair.Key);
                    builder.Append(':');
                    AppendJsonValue(builder, pair.Value);
                }

                builder.Append('}');
                return;
            }

            if (value.Kind == JsonValueKind.Array)
            {
                builder.Append('[');
                for (int index = 0;
                    value.ArrayValue != null && index < value.ArrayValue.Count;
                    index++)
                {
                    if (index > 0)
                    {
                        builder.Append(',');
                    }

                    AppendJsonValue(builder, value.ArrayValue[index]);
                }

                builder.Append(']');
                return;
            }

            if (value.Kind == JsonValueKind.String)
            {
                AppendString(builder, value.StringValue);
                return;
            }

            if (value.Kind == JsonValueKind.Number)
            {
                builder.Append(value.NumberValue ?? "0");
                return;
            }

            if (value.Kind == JsonValueKind.Boolean)
            {
                builder.Append(value.BooleanValue ? "true" : "false");
                return;
            }

            builder.Append("null");
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
