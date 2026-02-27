using System.Text;
using System.Text.Json;

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Deterministic JSON serialization with sorted keys and no whitespace.
    /// Matches the Rust SDK's canonical_json() and Dart's canonical_json.dart.
    /// </summary>
    public static class CanonicalJson
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Serializes an object to canonical JSON (sorted keys, no whitespace).
        /// </summary>
        public static string Serialize(object value)
        {
            var json = JsonSerializer.Serialize(value, DefaultOptions);
            using var doc = JsonDocument.Parse(json);
            return Serialize(doc.RootElement);
        }

        /// <summary>
        /// Serializes a JsonElement to canonical JSON (sorted keys, no whitespace).
        /// </summary>
        public static string Serialize(JsonElement element)
        {
            var sb = new StringBuilder();
            WriteCanonical(sb, element);
            return sb.ToString();
        }

        /// <summary>
        /// Serializes an object to canonical JSON as a UTF-8 byte array.
        /// </summary>
        public static byte[] SerializeToBytes(object value)
        {
            return Encoding.UTF8.GetBytes(Serialize(value));
        }

        private static void WriteCanonical(StringBuilder sb, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    WriteObject(sb, element);
                    break;

                case JsonValueKind.Array:
                    WriteArray(sb, element);
                    break;

                case JsonValueKind.String:
                    sb.Append(JsonSerializer.Serialize(element.GetString()));
                    break;

                case JsonValueKind.Number:
                    sb.Append(element.GetRawText());
                    break;

                case JsonValueKind.True:
                    sb.Append("true");
                    break;

                case JsonValueKind.False:
                    sb.Append("false");
                    break;

                case JsonValueKind.Null:
                    sb.Append("null");
                    break;
            }
        }

        private static void WriteObject(StringBuilder sb, JsonElement element)
        {
            // Sort properties alphabetically by key
            var properties = new List<(string Name, JsonElement Value)>();
            foreach (var prop in element.EnumerateObject())
            {
                properties.Add((prop.Name, prop.Value));
            }
            properties.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            sb.Append('{');
            for (int i = 0; i < properties.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonSerializer.Serialize(properties[i].Name));
                sb.Append(':');
                WriteCanonical(sb, properties[i].Value);
            }
            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, JsonElement element)
        {
            sb.Append('[');
            int i = 0;
            foreach (var item in element.EnumerateArray())
            {
                if (i > 0) sb.Append(',');
                WriteCanonical(sb, item);
                i++;
            }
            sb.Append(']');
        }
    }
}
