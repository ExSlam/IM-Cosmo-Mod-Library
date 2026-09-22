using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GraduationDetails
{
    /// <summary>
    /// The Unity 2019 player can omit collection fields on these dynamically loaded
    /// DTOs. Persist their public fields explicitly, preserving integer tokens and
    /// constructor defaults without depending on native Unity serialization.
    /// </summary>
    internal static class GraduationDetailsJson
    {
        private const int MaximumDepth = 64;
        private sealed class Number { internal string Text; }

        internal static string Serialize<T>(T document)
        {
            StringBuilder output = new StringBuilder();
            Write(output, document, 0);
            return output.ToString();
        }

        internal static T Deserialize<T>(string json) where T : class
        {
            object parsed = new Reader(json).ReadDocument();
            Dictionary<string, object> root = parsed as Dictionary<string, object>;
            if (root == null) throw new FormatException("Graduation Details JSON must contain an object.");
            Require(root, "FormatName");
            Require(root, "FormatVersion");
            if (typeof(T) == typeof(GraduationDetailsSidecarDocument))
            {
                Require(root, "RelativeSavePath");
                Require(root, "LastIssuedSequence");
            }
            // A header-only document from the old serializer is data loss, not a
            // valid empty snapshot. Refuse it before any materialized state changes.
            foreach (FieldInfo field in Fields(typeof(T)))
                if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    Require(root, field.Name);
                    if (!(root[field.Name] is List<object>))
                        throw new FormatException("Missing or invalid collection: " + field.Name);
                }
            return (T)ConvertValue(parsed, typeof(T), 0);
        }

        private static void Require(Dictionary<string, object> root, string name)
        {
            if (!root.ContainsKey(name) || root[name] == null)
                throw new FormatException("Graduation Details JSON is missing required member " + name + ".");
        }
        private static FieldInfo[] Fields(Type type)
        {
            if (type.Namespace != typeof(GraduationDetailsJson).Namespace || !type.IsSerializable)
                throw new FormatException("Unsupported Graduation Details DTO: " + type.FullName);
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsNotSerialized).OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
        }
        private static void Depth(int depth)
        {
            if (depth > MaximumDepth) throw new FormatException("Graduation Details JSON exceeds its nesting limit.");
        }
        private static void Write(StringBuilder output, object value, int depth)
        {
            Depth(depth);
            if (value == null) { output.Append("null"); return; }
            if (value is string) { Quote(output, (string)value); return; }
            if (value is bool) { output.Append((bool)value ? "true" : "false"); return; }
            Type type = value.GetType();
            if (type.IsEnum) { output.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture)); return; }
            if (value is int || value is long) { output.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture)); return; }
            if (value is float || value is double)
            {
                double numeric = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (Double.IsNaN(numeric) || Double.IsInfinity(numeric)) throw new FormatException("Non-finite JSON number.");
                output.Append(((IFormattable)value).ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            IList list = value as IList;
            if (list != null)
            {
                output.Append('[');
                for (int index = 0; index < list.Count; index++)
                {
                    if (index != 0) output.Append(',');
                    Write(output, list[index], depth + 1);
                }
                output.Append(']');
                return;
            }
            output.Append('{');
            bool first = true;
            foreach (FieldInfo field in Fields(type))
            {
                if (!first) output.Append(',');
                first = false;
                Quote(output, field.Name); output.Append(':');
                Write(output, field.GetValue(value), depth + 1);
            }
            output.Append('}');
        }
        private static void Quote(StringBuilder output, string value)
        {
            output.Append('"');
            foreach (char character in value)
            {
                if (character == '"' || character == '\\') { output.Append('\\'); output.Append(character); }
                else if (character < 32) { output.Append("\\u"); output.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture)); }
                else output.Append(character);
            }
            output.Append('"');
        }
        private static object ConvertValue(object value, Type type, int depth)
        {
            Depth(depth);
            if (value == null)
            {
                if (type.IsValueType) throw new FormatException("Null value for " + type.FullName);
                return null;
            }
            if (type == typeof(string) && value is string) return value;
            if (type == typeof(bool) && value is bool) return value;
            Number number = value as Number;
            if (number != null)
            {
                if (type.IsEnum) return Enum.ToObject(type, Int32.Parse(number.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
                if (type == typeof(int)) return Int32.Parse(number.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                if (type == typeof(long)) return Int64.Parse(number.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                if (type == typeof(float) || type == typeof(double))
                {
                    double numeric = Double.Parse(number.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (Double.IsNaN(numeric) || Double.IsInfinity(numeric)) throw new FormatException("Non-finite JSON number.");
                    if (type == typeof(double)) return numeric;
                    float single = (float)numeric;
                    if (Single.IsInfinity(single)) throw new FormatException("JSON float overflow.");
                    return single;
                }
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && value is List<object>)
            {
                IList result = (IList)Activator.CreateInstance(type);
                foreach (object item in (List<object>)value) result.Add(ConvertValue(item, type.GetGenericArguments()[0], depth + 1));
                return result;
            }
            Dictionary<string, object> fields = value as Dictionary<string, object>;
            if (fields != null)
            {
                FieldInfo[] contract = Fields(type);
                object result = Activator.CreateInstance(type, true);
                foreach (FieldInfo field in contract)
                {
                    object fieldValue;
                    if (fields.TryGetValue(field.Name, out fieldValue)) field.SetValue(result, ConvertValue(fieldValue, field.FieldType, depth + 1));
                }
                return result;
            }
            throw new FormatException("JSON value does not match " + type.FullName);
        }

        private sealed class Reader
        {
            private readonly string text;
            private int offset;
            internal Reader(string text) { this.text = text ?? ""; }
            internal object ReadDocument()
            {
                object result = Value(0);
                Space();
                if (offset != text.Length) throw Error("Trailing content");
                return result;
            }
            private FormatException Error(string reason) { return new FormatException(reason + " at JSON offset " + offset + "."); }
            private void Space() { while (offset < text.Length && (text[offset] == ' ' || text[offset] == '\r' || text[offset] == '\n' || text[offset] == '\t')) offset++; }
            private bool Take(char character)
            {
                Space();
                if (offset >= text.Length || text[offset] != character) return false;
                offset++; return true;
            }
            private void Expect(char character) { if (!Take(character)) throw Error("Expected " + character); }
            private object Value(int depth)
            {
                Depth(depth); Space();
                if (offset == text.Length) throw Error("Missing value");
                char current = text[offset];
                if (current == '"') return String();
                if (Take('{'))
                {
                    Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (Take('}')) return result;
                    do
                    {
                        string key = String(); Expect(':');
                        if (result.ContainsKey(key)) throw Error("Duplicate member " + key);
                        result.Add(key, Value(depth + 1));
                        if (Take('}')) return result;
                    } while (Take(','));
                    throw Error("Expected object delimiter");
                }
                if (Take('['))
                {
                    List<object> result = new List<object>();
                    if (Take(']')) return result;
                    do
                    {
                        result.Add(Value(depth + 1));
                        if (Take(']')) return result;
                    } while (Take(','));
                    throw Error("Expected array delimiter");
                }
                if (current == 't') { Literal("true"); return true; }
                if (current == 'f') { Literal("false"); return false; }
                if (current == 'n') { Literal("null"); return null; }
                int start = offset;
                if (text[offset] == '-') offset++;
                if (offset < text.Length && text[offset] == '0') offset++;
                else Digits();
                if (offset < text.Length && text[offset] == '.') { offset++; Digits(); }
                if (offset < text.Length && (text[offset] == 'e' || text[offset] == 'E'))
                {
                    offset++;
                    if (offset < text.Length && (text[offset] == '-' || text[offset] == '+')) offset++;
                    Digits();
                }
                return new Number { Text = text.Substring(start, offset - start) };
            }
            private void Digits()
            {
                int start = offset;
                while (offset < text.Length && text[offset] >= '0' && text[offset] <= '9') offset++;
                if (start == offset) throw Error("Expected digit");
            }
            private void Literal(string value)
            {
                if (offset + value.Length > text.Length || System.String.CompareOrdinal(text, offset, value, 0, value.Length) != 0) throw Error("Invalid literal");
                offset += value.Length;
            }
            private string String()
            {
                Expect('"');
                StringBuilder result = new StringBuilder();
                while (offset < text.Length)
                {
                    char current = text[offset++];
                    if (current == '"') return result.ToString();
                    if (current < 32) throw Error("Unescaped control character");
                    if (current != '\\') { result.Append(current); continue; }
                    if (offset == text.Length) throw Error("Incomplete escape");
                    current = text[offset++];
                    switch (current)
                    {
                        case '"': case '\\': case '/': result.Append(current); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u':
                            if (offset + 4 > text.Length) throw Error("Incomplete Unicode escape");
                            result.Append((char)UInt16.Parse(text.Substring(offset, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                            offset += 4; break;
                        default: throw Error("Invalid escape");
                    }
                }
                throw Error("Unterminated string");
            }
        }
    }
}
