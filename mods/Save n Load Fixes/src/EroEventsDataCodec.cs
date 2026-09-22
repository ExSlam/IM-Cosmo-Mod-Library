using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SaveNLoadFixes.Persistence;
using JsonValue = SaveNLoadFixes.Persistence.RepairEnvelopeCodec.FiniteJsonValue;
using JsonKind = SaveNLoadFixes.Persistence.RepairEnvelopeCodec.FiniteJsonKind;

namespace SaveNLoadFixes
{
    /// <summary>
    /// Opt-in EroEvents integer payload contract. V1 contains Int32 values; V2
    /// contains Int64 values. Both use exact decimal strings on write and accept
    /// integer JSON tokens on read. No double, truncation, saturation or unchecked
    /// narrowing occurs. This does not change EroEvents' existing outfit IDs/flags.
    /// </summary>
    public static class EroEventsDataCodec
    {
        public const string Owner = "com.seraph.eroevents";

        public static ModPayload EncodeInt64(IDictionary<string, long> values)
        { return Encode(values, 2); }
        public static ModPayload EncodeInt32(IDictionary<string, int> values)
        {
            if (values == null) throw new ArgumentNullException("values");
            Dictionary<string, long> wide = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in values) wide.Add(pair.Key, pair.Value);
            return Encode(wide, 1);
        }
        private static ModPayload Encode(IDictionary<string, long> values, int schema)
        {
            if (values == null) throw new ArgumentNullException("values");
            StringBuilder json = new StringBuilder("{\"integers\":{");
            bool comma = false;
            foreach (KeyValuePair<string, long> pair in new SortedDictionary<string, long>(values, StringComparer.Ordinal))
            {
                if (comma) json.Append(',');
                comma = true;
                RepairEnvelopeCodec.AppendJsonString(json, pair.Key);
                json.Append(':');
                RepairEnvelopeCodec.AppendJsonString(json, pair.Value.ToString(CultureInfo.InvariantCulture));
            }
            return new ModPayload(schema, json.Append("}}").ToString());
        }
        public static bool TryDecodeInt64(ModPayload payload, out Dictionary<string, long> values, out string error)
        {
            values = null; error = string.Empty;
            JsonValue root, integers;
            if (payload == null || (payload.SchemaVersion != 1 && payload.SchemaVersion != 2))
            { error = "Unsupported EroEvents integer schema."; return false; }
            if (!RepairEnvelopeCodec.FiniteJsonParser.TryParse(payload.Json, out root, out error) || root.Kind != JsonKind.Object ||
                root.ObjectValues.Count != 1 || !root.ObjectValues.TryGetValue("integers", out integers) || integers.Kind != JsonKind.Object)
            { error = "Expected an EroEvents integers object."; return false; }
            Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> pair in integers.ObjectValues)
            {
                long number;
                if ((pair.Value.Kind != JsonKind.String && pair.Value.Kind != JsonKind.Number) ||
                    !long.TryParse(pair.Value.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number) ||
                    pair.Value.Text != number.ToString(CultureInfo.InvariantCulture) ||
                    (payload.SchemaVersion == 1 && (number < int.MinValue || number > int.MaxValue)))
                { error = "Invalid/out-of-range EroEvents integer: " + pair.Key; return false; }
                result.Add(pair.Key, number);
            }
            values = result;
            return true;
        }
        public static bool TryDecodeInt32(ModPayload payload, out Dictionary<string, int> values, out string error)
        {
            values = null;
            Dictionary<string, long> wide;
            if (!TryDecodeInt64(payload, out wide, out error)) return false;
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, long> pair in wide)
            {
                if (pair.Value < int.MinValue || pair.Value > int.MaxValue)
                { error = "EroEvents Int64 value cannot fit Int32: " + pair.Key; return false; }
                result.Add(pair.Key, checked((int)pair.Value));
            }
            values = result;
            return true;
        }
    }
}
