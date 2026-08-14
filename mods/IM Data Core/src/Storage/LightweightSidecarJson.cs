using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        /// <summary>
        /// Streams one sidecar directly to a writer. This keeps peak save-time
        /// memory proportional to the largest individual record instead of the
        /// complete campaign history.
        /// </summary>
        internal static void SerializeTo(
            TextWriter writer,
            LightweightSidecarDocument document)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            ValidateSerializableDocument(document);

            StringBuilder fragment = new StringBuilder(512);
            writer.Write('{');

            fragment.Length = 0;
            AppendPropertyName(fragment, "FormatName");
            AppendString(fragment, document.FormatName ?? string.Empty);
            fragment.Append(',');
            AppendPropertyName(fragment, "FormatVersion");
            AppendInt32(fragment, document.FormatVersion);
            fragment.Append(',');
            AppendPropertyName(fragment, "RelativeSavePath");
            AppendString(fragment, document.RelativeSavePath ?? string.Empty);
            fragment.Append(',');
            AppendPropertyName(fragment, "LastIssuedSequence");
            AppendInt64(fragment, document.LastIssuedSequence);
            fragment.Append(',');
            AppendPropertyName(fragment, "Checkpoints");
            writer.Write(fragment.ToString());

            writer.Write('[');
            for (int index = 0; index < document.Checkpoints.Count; index++)
            {
                if (index > 0)
                {
                    writer.Write(',');
                }
                fragment.Length = 0;
                AppendCheckpointRecord(fragment, document.Checkpoints[index]);
                writer.Write(fragment.ToString());
            }
            writer.Write(']');
            writer.Write(',');

            fragment.Length = 0;
            AppendPropertyName(fragment, "Events");
            writer.Write(fragment.ToString());
            writer.Write('[');
            for (int index = 0; index < document.Events.Count; index++)
            {
                if (index > 0)
                {
                    writer.Write(',');
                }
                fragment.Length = 0;
                AppendEventRecord(fragment, document.Events[index]);
                writer.Write(fragment.ToString());
            }
            writer.Write(']');
            writer.Write(',');

            fragment.Length = 0;
            AppendPropertyName(fragment, "CustomMutations");
            writer.Write(fragment.ToString());
            writer.Write('[');
            for (int index = 0; index < document.CustomMutations.Count; index++)
            {
                if (index > 0)
                {
                    writer.Write(',');
                }
                fragment.Length = 0;
                AppendCustomMutationRecord(
                    fragment,
                    document.CustomMutations[index]);
                writer.Write(fragment.ToString());
            }
            writer.Write(']');
            writer.Write('}');
        }

        private static void ValidateSerializableDocument(
            LightweightSidecarDocument document)
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

            string formatName = RequireString(root, "FormatName");
            int formatVersion = RequireInt32(root, "FormatVersion");
            string relativeSavePath = RequireString(root, "RelativeSavePath");

            return new LightweightSidecarDocument
            {
                FormatName = formatName,
                FormatVersion = formatVersion,
                RelativeSavePath = relativeSavePath,
                LastIssuedSequence = RequireInt64(root, "LastIssuedSequence"),
                Checkpoints = ReadCheckpoints(
                    RequireArray(root, "Checkpoints"),
                    formatVersion,
                    relativeSavePath),
                Events = ReadEvents(
                    RequireArray(root, "Events"),
                    formatVersion),
                CustomMutations = ReadCustomMutations(
                    RequireArray(root, "CustomMutations"),
                    formatVersion)
            };
        }

        /// <summary>
        /// Validates and normalizes one arbitrary JSON document. The public API
        /// still exchanges JSON as strings because that is a convenient mod boundary,
        /// while the v3 sidecar stores the parsed value structurally.
        /// </summary>
        internal static bool TryNormalizeJsonDocument(
            string json,
            out string normalizedJson,
            out string errorMessage)
        {
            normalizedJson = string.Empty;
            errorMessage = string.Empty;
            if (json == null)
            {
                errorMessage = "JSON value cannot be null.";
                return false;
            }

            try
            {
                JsonValue value = new JsonParser(json).ParseDocument();
                normalizedJson = SerializeJsonValue(value);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = "The value is not valid JSON: " + exception.Message;
                return false;
            }
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

        internal static string ExpandSingleReleasePayloadForPublic(
            string payloadJson,
            int idolId,
            int positionIndex,
            int rowIndex,
            bool isCenter)
        {
            if (string.IsNullOrEmpty(payloadJson) ||
                idolId < CoreConstants.MinimumValidIdolIdentifier ||
                positionIndex < CoreConstants.ZeroBasedListStartIndex ||
                rowIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue payloadValue =
                    new JsonParser(payloadJson).ParseDocument();
                if (payloadValue == null ||
                    payloadValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                // Keep the ordered cast list public: it is the minimal historical
                // source needed to rebuild the complete release-time senbatsu,
                // including members vanilla later replaces with null.
                payloadValue.ObjectValue[CoreConstants.JsonFieldIdolId] =
                    CreateNumberValue(idolId);
                payloadValue.ObjectValue[CoreConstants.JsonFieldPositionIndex] =
                    CreateNumberValue(positionIndex);
                payloadValue.ObjectValue[CoreConstants.JsonFieldRowIndex] =
                    CreateNumberValue(rowIndex);
                payloadValue.ObjectValue[CoreConstants.JsonFieldIsCenter] =
                    new JsonValue
                    {
                        Kind = JsonValueKind.Boolean,
                        BooleanValue = isCenter
                    };

                return SerializeJsonValue(payloadValue);
            }
            catch
            {
                // Public projection is fail-soft. The stored history remains
                // unchanged if a future payload shape cannot be expanded.
                return payloadJson;
            }
        }

        /// <summary>
        /// Adds or replaces integer fields on a cloned public payload. Shared
        /// timeline rows use this to reconstruct the small per-idol projection
        /// while leaving the persisted common snapshot unchanged.
        /// </summary>
        internal static string ExpandIntegerPayloadForPublic(
            string payloadJson,
            IReadOnlyDictionary<string, long> projectedValues)
        {
            if (string.IsNullOrEmpty(payloadJson) ||
                projectedValues == null ||
                projectedValues.Count == 0)
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue payloadValue =
                    new JsonParser(payloadJson).ParseDocument();
                if (payloadValue == null ||
                    payloadValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                foreach (KeyValuePair<string, long> projectedValue
                    in projectedValues)
                {
                    if (string.IsNullOrEmpty(projectedValue.Key))
                    {
                        continue;
                    }

                    payloadValue.ObjectValue[projectedValue.Key] =
                        CreateNumberValue(projectedValue.Value);
                }

                return SerializeJsonValue(payloadValue);
            }
            catch
            {
                // Projection is an API convenience. Never alter or reject the
                // durable row when a future payload shape is not understood.
                return payloadJson;
            }
        }

        private static JsonValue CreateNumberValue(int value)
        {
            return CreateNumberValue((long)value);
        }

        private static JsonValue CreateNumberValue(long value)
        {
            return new JsonValue
            {
                Kind = JsonValueKind.Number,
                NumberValue = value.ToString(CultureInfo.InvariantCulture)
            };
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
                // Future fields are retained until this compactor explicitly
                // knows their declared default, preventing accidental data loss.
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
                List<string> propertyNames = value.ObjectValue == null
                    ? new List<string>()
                    : new List<string>(value.ObjectValue.Keys);
                propertyNames.Sort(StringComparer.Ordinal);

                for (int index = 0; index < propertyNames.Count; index++)
                {
                    string propertyName = propertyNames[index];
                    JsonValue propertyValue = value.ObjectValue[propertyName];
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    AppendString(builder, propertyName);
                    builder.Append(':');
                    AppendJsonValue(builder, propertyValue);
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
                AppendCheckpointRecord(builder, records[index]);
            }
            builder.Append(']');
        }

        private static void AppendCheckpointRecord(
            StringBuilder builder,
            LightweightCheckpointRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar contains a null checkpoint record.");
            }

            builder.Append('{');
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
                AppendEventRecord(builder, records[index]);
            }
            builder.Append(']');
        }

        private static void AppendEventRecord(
            StringBuilder builder,
            LightweightEventRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar contains a null event record.");
            }

            builder.Append('{');
            AppendPropertyName(builder, "Sequence");
            AppendInt64(builder, record.Sequence);
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
            if (!string.IsNullOrEmpty(record.IdempotencyKey))
            {
                builder.Append(',');
                AppendPropertyName(builder, "IdempotencyKey");
                AppendString(builder, record.IdempotencyKey);
            }
            builder.Append(',');
            AppendPropertyName(builder, "Payload");

            JsonValue payloadValue = ParseJsonForStorage(
                record.PayloadJson,
                "An event payload");
            if (string.IsNullOrEmpty(record.NamespaceIdentifier))
            {
                TransformEventPayloadForStorage(payloadValue);
            }
            AppendJsonValue(builder, payloadValue);
            builder.Append('}');
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
                AppendCustomMutationRecord(builder, records[index]);
            }
            builder.Append(']');
        }

        private static void AppendCustomMutationRecord(
            StringBuilder builder,
            LightweightCustomMutationRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar contains a null custom mutation.");
            }

            builder.Append('{');
            AppendPropertyName(builder, "Sequence");
            AppendInt64(builder, record.Sequence);
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

            if (string.Equals(
                    record.Operation,
                    LightweightCoreStorageEngine.CustomOperationSet,
                    StringComparison.Ordinal))
            {
                builder.Append(',');
                AppendPropertyName(builder, "Value");
                AppendJsonValue(
                    builder,
                    ParseJsonForStorage(
                        record.ValueJson,
                        "A custom-data value"));
            }

            builder.Append('}');
        }


        private static List<LightweightCheckpointRecord> ReadCheckpoints(
            List<JsonValue> values,
            int formatVersion,
            string documentRelativeSavePath)
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
                        RelativeSavePath = formatVersion >= 3
                            ? documentRelativeSavePath ?? string.Empty
                            : RequireString(item, "RelativeSavePath"),
                        LastSave = RequireString(item, "LastSave"),
                        PlaytimeSeconds = RequireInt64(item, "PlaytimeSeconds"),
                        GameDateTime = RequireString(item, "GameDateTime"),
                        Sequence = RequireInt64(item, "Sequence")
                    });
            }

            return records;
        }


        private static List<LightweightEventRecord> ReadEvents(
            List<JsonValue> values,
            int formatVersion)
        {
            List<LightweightEventRecord> records =
                new List<LightweightEventRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "An event entry must be a JSON object.");

                long sequence = RequireInt64(item, "Sequence");
                string gameDateTime = RequireString(item, "GameDateTime");
                string namespaceIdentifier = RequireString(
                    item,
                    "NamespaceIdentifier");
                int gameDateKey;
                string payloadJson;

                if (formatVersion >= 3)
                {
                    gameDateKey = BuildGameDateKeyFromRoundTrip(
                        gameDateTime,
                        "event");
                    JsonValue payloadValue = RequireMember(item, "Payload");
                    if (string.IsNullOrEmpty(namespaceIdentifier))
                    {
                        TransformEventPayloadForRuntime(payloadValue);
                    }
                    payloadJson = SerializeJsonValue(payloadValue);
                }
                else
                {
                    long storedEventId = RequireInt64(item, "EventId");
                    if (storedEventId != sequence)
                    {
                        throw new FormatException(
                            "The legacy lightweight sidecar contains an event " +
                            "identifier that does not match its sequence.");
                    }

                    int storedGameDateKey = RequireInt32(item, "GameDateKey");
                    gameDateKey = BuildGameDateKeyFromRoundTrip(
                        gameDateTime,
                        "event");
                    if (storedGameDateKey != gameDateKey)
                    {
                        throw new FormatException(
                            "The legacy lightweight sidecar contains an event " +
                            "GameDateKey that does not match GameDateTime.");
                    }

                    payloadJson = RequireString(item, "PayloadJson");
                }

                records.Add(
                    new LightweightEventRecord
                    {
                        Sequence = sequence,
                        GameDateKey = gameDateKey,
                        GameDateTime = gameDateTime,
                        IdolId = RequireInt32(item, "IdolId"),
                        EntityKind = RequireString(item, "EntityKind"),
                        EntityId = RequireString(item, "EntityId"),
                        EventType = RequireString(item, "EventType"),
                        SourcePatch = RequireString(item, "SourcePatch"),
                        NamespaceIdentifier = namespaceIdentifier,
                        IdempotencyKey = ReadOptionalString(
                            item,
                            "IdempotencyKey"),
                        PayloadJson = payloadJson
                    });
            }

            return records;
        }



        private static List<LightweightCustomMutationRecord> ReadCustomMutations(
            List<JsonValue> values,
            int formatVersion)
        {
            List<LightweightCustomMutationRecord> records =
                new List<LightweightCustomMutationRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "A custom mutation entry must be a JSON object.");

                string operation = RequireString(item, "Operation");
                string gameDateTime = RequireString(item, "GameDateTime");
                int gameDateKey;
                string valueJson;

                if (formatVersion >= 3)
                {
                    gameDateKey = BuildGameDateKeyFromRoundTrip(
                        gameDateTime,
                        "custom-data mutation");

                    valueJson = string.Equals(
                        operation,
                        LightweightCoreStorageEngine.CustomOperationSet,
                        StringComparison.Ordinal)
                        ? SerializeJsonValue(RequireMember(item, "Value"))
                        : string.Empty;
                }
                else
                {
                    int storedGameDateKey = RequireInt32(item, "GameDateKey");
                    gameDateKey = BuildGameDateKeyFromRoundTrip(
                        gameDateTime,
                        "custom-data mutation");
                    if (storedGameDateKey != gameDateKey)
                    {
                        throw new FormatException(
                            "The legacy lightweight sidecar contains a custom-data " +
                            "GameDateKey that does not match GameDateTime.");
                    }

                    valueJson = RequireString(item, "ValueJson");
                }

                records.Add(
                    new LightweightCustomMutationRecord
                    {
                        Sequence = RequireInt64(item, "Sequence"),
                        GameDateKey = gameDateKey,
                        GameDateTime = gameDateTime,
                        NamespaceIdentifier = RequireString(
                            item,
                            "NamespaceIdentifier"),
                        DataKey = RequireString(item, "DataKey"),
                        Operation = operation,
                        ValueJson = valueJson
                    });
            }

            return records;
        }


        private static JsonValue ParseJsonForStorage(
            string json,
            string description)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException(
                    (description ?? "A JSON value") + " is empty.");
            }

            try
            {
                return new JsonParser(json).ParseDocument();
            }
            catch (Exception exception)
            {
                throw new FormatException(
                    (description ?? "A JSON value") +
                    " is not valid JSON: " +
                    exception.Message,
                    exception);
            }
        }

        private static int BuildGameDateKeyFromRoundTrip(
            string gameDateTime,
            string recordKind)
        {
            DateTime parsed;
            if (!DateTime.TryParseExact(
                    gameDateTime ?? string.Empty,
                    CoreConstants.RoundTripDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsed))
            {
                throw new FormatException(
                    "The lightweight sidecar " +
                    (recordKind ?? "record") +
                    " has an invalid GameDateTime.");
            }

            return CoreDateTimeUtility.BuildGameDateKey(parsed);
        }

        /// <summary>
        /// Makes built-in event payloads document-native on disk while keeping the
        /// established string-based public payload contract. Only built-in IMDC event payloads are rewritten; namespaced custom-event JSON is left untouched.
        /// </summary>
        private static void TransformEventPayloadForStorage(JsonValue value)
        {
            if (value == null)
            {
                return;
            }

            if (value.Kind == JsonValueKind.Array)
            {
                for (int index = 0;
                    value.ArrayValue != null && index < value.ArrayValue.Count;
                    index++)
                {
                    TransformEventPayloadForStorage(value.ArrayValue[index]);
                }
                return;
            }

            if (value.Kind != JsonValueKind.Object ||
                value.ObjectValue == null)
            {
                return;
            }

            JsonValue detailJson;
            if (value.ObjectValue.TryGetValue("detail_json", out detailJson) &&
                detailJson != null &&
                detailJson.Kind == JsonValueKind.String)
            {
                if (string.IsNullOrWhiteSpace(detailJson.StringValue))
                {
                    value.ObjectValue.Remove("detail_json");
                }
                else
                {
                    try
                    {
                        JsonValue detailValue =
                            new JsonParser(detailJson.StringValue).ParseDocument();
                        value.ObjectValue.Remove("detail_json");
                        value.ObjectValue["detail"] = detailValue;
                    }
                    catch
                    {
                        // A malformed historical detail payload is left untouched.
                    }
                }
            }

            List<string> propertyNames =
                new List<string>(value.ObjectValue.Keys);
            for (int index = 0; index < propertyNames.Count; index++)
            {
                string propertyName = propertyNames[index];
                JsonValue member;
                if (!value.ObjectValue.TryGetValue(propertyName, out member) ||
                    member == null)
                {
                    continue;
                }

                if (member.Kind == JsonValueKind.String &&
                    IsDelimitedListProperty(propertyName))
                {
                    value.ObjectValue[propertyName] =
                        ParseDelimitedListForStorage(
                            propertyName,
                            member.StringValue);
                    continue;
                }

                TransformEventPayloadForStorage(member);
            }
        }

        /// <summary>
        /// Reconstructs the stable public/runtime payload shape from the native v3
        /// disk representation. This keeps existing Cosmo consumers source-compatible
        /// without storing JSON or identifier arrays as quoted strings.
        /// </summary>
        private static void TransformEventPayloadForRuntime(JsonValue value)
        {
            if (value == null)
            {
                return;
            }

            if (value.Kind == JsonValueKind.Array)
            {
                for (int index = 0;
                    value.ArrayValue != null && index < value.ArrayValue.Count;
                    index++)
                {
                    TransformEventPayloadForRuntime(value.ArrayValue[index]);
                }
                return;
            }

            if (value.Kind != JsonValueKind.Object ||
                value.ObjectValue == null)
            {
                return;
            }

            JsonValue detail;
            if (value.ObjectValue.TryGetValue("detail", out detail) &&
                detail != null &&
                !value.ObjectValue.ContainsKey("detail_json"))
            {
                // Restore any JSON-native list fields inside the detail object
                // before re-encoding the established public detail_json view.
                TransformEventPayloadForRuntime(detail);
                value.ObjectValue.Remove("detail");
                value.ObjectValue["detail_json"] = new JsonValue
                {
                    Kind = JsonValueKind.String,
                    StringValue = SerializeJsonValue(detail)
                };
            }

            List<string> propertyNames =
                new List<string>(value.ObjectValue.Keys);
            for (int index = 0; index < propertyNames.Count; index++)
            {
                string propertyName = propertyNames[index];
                JsonValue member;
                if (!value.ObjectValue.TryGetValue(propertyName, out member) ||
                    member == null)
                {
                    continue;
                }

                if (member.Kind == JsonValueKind.Array &&
                    IsDelimitedListProperty(propertyName))
                {
                    value.ObjectValue[propertyName] = new JsonValue
                    {
                        Kind = JsonValueKind.String,
                        StringValue = SerializeDelimitedListForRuntime(
                            member.ArrayValue)
                    };
                    continue;
                }

                TransformEventPayloadForRuntime(member);
            }
        }

        private static bool IsDelimitedListProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            string normalized = propertyName
                .Replace("_", string.Empty)
                .ToLowerInvariant();

            return normalized.IndexOf(
                    "idlist",
                    StringComparison.Ordinal) >= 0 ||
                string.Equals(
                    normalized,
                    "removedtaskcustomlist",
                    StringComparison.Ordinal);
        }

        private static JsonValue ParseDelimitedListForStorage(
            string propertyName,
            string rawValue)
        {
            JsonValue array = new JsonValue
            {
                Kind = JsonValueKind.Array,
                ArrayValue = new List<JsonValue>()
            };

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return array;
            }

            bool identifierList =
                (propertyName ?? string.Empty)
                    .Replace("_", string.Empty)
                    .ToLowerInvariant()
                    .IndexOf(
                        "idlist",
                        StringComparison.Ordinal) >= 0;

            string[] tokens = rawValue.Split(',');
            for (int index = 0; index < tokens.Length; index++)
            {
                string token = tokens[index].Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                long identifier;
                if (identifierList &&
                    long.TryParse(
                        token,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out identifier))
                {
                    array.ArrayValue.Add(
                        new JsonValue
                        {
                            Kind = JsonValueKind.Number,
                            NumberValue = identifier.ToString(
                                CultureInfo.InvariantCulture)
                        });
                }
                else
                {
                    array.ArrayValue.Add(
                        new JsonValue
                        {
                            Kind = JsonValueKind.String,
                            StringValue = token
                        });
                }
            }

            return array;
        }

        private static string SerializeDelimitedListForRuntime(
            List<JsonValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < values.Count; index++)
            {
                JsonValue value = values[index];
                if (value == null)
                {
                    continue;
                }

                string token;
                if (value.Kind == JsonValueKind.Number)
                {
                    token = value.NumberValue ?? string.Empty;
                }
                else if (value.Kind == JsonValueKind.String)
                {
                    token = value.StringValue ?? string.Empty;
                }
                else
                {
                    token = SerializeJsonValue(value);
                }

                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(token);
            }

            return builder.ToString();
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

        private static string ReadOptionalString(
            Dictionary<string, JsonValue> source,
            string propertyName)
        {
            JsonValue value;
            if (source == null ||
                !source.TryGetValue(propertyName, out value))
            {
                return string.Empty;
            }

            if (value == null || value.Kind != JsonValueKind.String)
            {
                throw new FormatException(
                    "JSON property '" + propertyName + "' must be a string when present.");
            }

            return value.StringValue ?? string.Empty;
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
