using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class HashUtil
{
    public static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public static string Sha256Hex(string text) => Sha256Hex(Encoding.UTF8.GetBytes(text));
    public static string Sha256Prefixed(string text) => "sha256:" + Sha256Hex(text.Replace("\r\n", "\n").Replace("\r", "\n"));
}

internal static class JsonUtil
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    public static JsonNode ParseNode(string json, string description)
    {
        try { return JsonNode.Parse(json) ?? throw new InvalidDataException(description + " is JSON null."); }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        { throw new InvalidDataException(description + " is not valid JSON: " + ex.Message, ex); }
    }

    public static string Compact(JsonNode node) => node.ToJsonString(CompactOptions);
    public static string Pretty<T>(T value) => JsonSerializer.Serialize(value, PrettyOptions);

    // Unity JsonUtility.ToJson(savedData, false) preserves declaration/property order and lexical values.
    // Vanilla save files are the pretty-printed spelling of that same JSON graph, so removing only JSON
    // whitespace outside strings reproduces the compact byte stream without reordering properties or
    // changing floating point lexemes.
    public static string StripInsignificantWhitespace(string json)
    {
        var sb = new StringBuilder(json.Length);
        bool inString = false, escaped = false;
        foreach (char ch in json)
        {
            if (inString)
            {
                sb.Append(ch);
                if (escaped) escaped = false;
                else if (ch == '\\') escaped = true;
                else if (ch == '"') inString = false;
                continue;
            }
            if (ch == '"') { inString = true; sb.Append(ch); }
            else if (!char.IsWhiteSpace(ch)) sb.Append(ch);
        }
        if (inString) throw new InvalidDataException("Vanilla save contains an unterminated JSON string.");
        return sb.ToString();
    }

    public static string GetString(JsonObject obj, string name)
    {
        if (!obj.TryGetPropertyValue(name, out JsonNode? node) || node is null) return "";
        return node.GetValue<string?>() ?? "";
    }

    public static long GetInt64(JsonObject obj, string name, long fallback = 0)
    {
        if (!obj.TryGetPropertyValue(name, out JsonNode? node) || node is null) return fallback;
        if (node is JsonValue value && value.TryGetValue<long>(out long number)) return number;
        return fallback;
    }
}
