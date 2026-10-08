using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.V3
{
    /// <summary>
    /// A protocol duration. The node emits <c>{ "seconds": n, "nanoseconds": n }</c>; it also
    /// accepts a number of seconds or a Go duration string such as <c>"1s"</c> or <c>"1m30s"</c>,
    /// and this type reads all three.
    /// </summary>
    [JsonConverter(typeof(ProtocolDurationConverter))]
    public readonly record struct ProtocolDuration(ulong Seconds, ulong Nanoseconds)
    {
        public TimeSpan ToTimeSpan() =>
            TimeSpan.FromSeconds(Seconds) + TimeSpan.FromTicks((long)(Nanoseconds / 100));

        public static ProtocolDuration FromTimeSpan(TimeSpan t)
        {
            if (t <= TimeSpan.Zero) return default;
            var totalNs = (ulong)t.Ticks * 100UL;
            return new ProtocolDuration(totalNs / 1_000_000_000UL, totalNs % 1_000_000_000UL);
        }

        /// <summary>Parse a Go duration string (units ns, us, µs, ms, s, m, h; e.g. "1h2m3.5s").</summary>
        public static ProtocolDuration ParseGo(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("empty duration");
            var rest = s.Trim();
            if (rest == "0") return default;
            double totalNs = 0;
            while (rest.Length > 0)
            {
                int i = 0;
                while (i < rest.Length && (char.IsDigit(rest[i]) || rest[i] == '.')) i++;
                if (i == 0) throw new FormatException($"invalid duration: {s}");
                var num = double.Parse(rest[..i], CultureInfo.InvariantCulture);
                rest = rest[i..];
                int j = 0;
                while (j < rest.Length && !char.IsDigit(rest[j]) && rest[j] != '.') j++;
                var unit = rest[..j];
                rest = rest[j..];
                double mult = unit switch
                {
                    "ns" => 1,
                    "us" or "µs" or "μs" => 1e3,
                    "ms" => 1e6,
                    "s" => 1e9,
                    "m" => 60e9,
                    "h" => 3600e9,
                    _ => throw new FormatException($"unknown unit '{unit}' in duration: {s}"),
                };
                totalNs += num * mult;
            }
            var ns = (ulong)Math.Round(totalNs);
            return new ProtocolDuration(ns / 1_000_000_000UL, ns % 1_000_000_000UL);
        }
    }

    internal sealed class ProtocolDurationConverter : JsonConverter<ProtocolDuration>
    {
        public override ProtocolDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    {
                        var secs = reader.GetDouble();
                        if (secs <= 0) return default;
                        var whole = (ulong)Math.Floor(secs);
                        return new ProtocolDuration(whole, (ulong)Math.Round((secs - whole) * 1e9));
                    }
                case JsonTokenType.String:
                    return ProtocolDuration.ParseGo(reader.GetString()!);
                case JsonTokenType.StartObject:
                    {
                        ulong sec = 0, ns = 0;
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                        {
                            var name = reader.GetString();
                            reader.Read();
                            if (string.Equals(name, "seconds", StringComparison.OrdinalIgnoreCase)) sec = reader.GetUInt64();
                            else if (string.Equals(name, "nanoseconds", StringComparison.OrdinalIgnoreCase)) ns = reader.GetUInt64();
                            else reader.Skip();
                        }
                        return new ProtocolDuration(sec, ns);
                    }
                default:
                    throw new JsonException($"cannot read a duration from {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, ProtocolDuration value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Seconds != 0) writer.WriteNumber("seconds", value.Seconds);
            if (value.Nanoseconds != 0) writer.WriteNumber("nanoseconds", value.Nanoseconds);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Network-wide parameters (Go <c>protocol.NetworkGlobals</c>), as found in the network definition
    /// and network status. Fields the SDK does not interpret (thresholds, fee schedule, limits) are kept
    /// as raw JSON; unknown future fields are preserved in <see cref="ExtensionData"/>.
    /// </summary>
    public class NetworkGlobals
    {
        [JsonPropertyName("operatorAcceptThreshold")] public JsonElement? OperatorAcceptThreshold { get; set; }
        [JsonPropertyName("validatorAcceptThreshold")] public JsonElement? ValidatorAcceptThreshold { get; set; }

        /// <summary>Cron expression defining the (approximate) major block interval.</summary>
        [JsonPropertyName("majorBlockSchedule")] public string? MajorBlockSchedule { get; set; }

        [JsonPropertyName("anchorEmptyBlocks")] public bool AnchorEmptyBlocks { get; set; }
        [JsonPropertyName("feeSchedule")] public JsonElement? FeeSchedule { get; set; }
        [JsonPropertyName("limits")] public JsonElement? Limits { get; set; }

        /// <summary>
        /// The cadence the network produces blocks at (Accumulate 1.4.6.5+). Absent on networks that
        /// predate it, so older responses still parse.
        /// </summary>
        [JsonPropertyName("blockInterval")] public ProtocolDuration? BlockInterval { get; set; }

        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        public static NetworkGlobals FromJson(JsonElement element) =>
            JsonSerializer.Deserialize<NetworkGlobals>(element.GetRawText())
            ?? throw new JsonException("networkGlobals was null");
    }
}
