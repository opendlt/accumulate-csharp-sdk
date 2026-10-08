using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.V3
{
    /// <summary>
    /// A v3 API receipt: a merkle receipt together with the block it was produced against.
    /// Matches Go <c>api.Receipt</c> (pkg/api/v3/types.yml). Query results are untyped
    /// <see cref="JsonElement"/>s; use <see cref="FromJson"/> on the <c>receipt</c> object for typed access.
    /// </summary>
    public class Receipt
    {
        // ---- the embedded merkle receipt ----

        /// <summary>Hex of the hash the receipt starts at.</summary>
        [JsonPropertyName("start")] public string? Start { get; set; }
        [JsonPropertyName("startIndex")] public long StartIndex { get; set; }

        /// <summary>Hex of the hash the receipt ends at.</summary>
        [JsonPropertyName("end")] public string? End { get; set; }
        [JsonPropertyName("endIndex")] public long EndIndex { get; set; }

        /// <summary>Hex of the anchor (the root the receipt proves into).</summary>
        [JsonPropertyName("anchor")] public string? Anchor { get; set; }

        /// <summary>Merkle path entries, each <c>{ "right": bool, "hash": hex }</c>.</summary>
        [JsonPropertyName("entries")] public List<JsonElement>? Entries { get; set; }

        // ---- block context ----

        [JsonPropertyName("localBlock")] public ulong LocalBlock { get; set; }
        [JsonPropertyName("localBlockTime")] public DateTimeOffset? LocalBlockTime { get; set; }
        [JsonPropertyName("majorBlock")] public ulong MajorBlock { get; set; }

        /// <summary>The minor block height the receipt was produced against; 0 means the current state.</summary>
        [JsonPropertyName("forHeight")] public ulong ForHeight { get; set; }

        /// <summary>
        /// The receipt terminates at a directory root, so there is no second call to make. When false,
        /// <see cref="Partition"/> names the BPT root it ends at and the caller continues with
        /// <c>AnchorReceiptAsync</c>.
        /// </summary>
        [JsonPropertyName("complete")] public bool Complete { get; set; }

        /// <summary>When not <see cref="Complete"/>, whose BPT root the receipt terminates at.</summary>
        [JsonPropertyName("partition")] public string? Partition { get; set; }

        /// <summary>
        /// Set only on a historical receipt: the receipt starts at a plain hash of the account's main
        /// state, and the account served beside it is that state as of <see cref="ForHeight"/>. Without
        /// it the receipt starts at the account's whole BPT entry and no account body is served.
        /// </summary>
        [JsonPropertyName("startsAtMainState")] public bool StartsAtMainState { get; set; }

        /// <summary>Deserialize a receipt object as returned by the v3 API.</summary>
        public static Receipt FromJson(JsonElement element) =>
            JsonSerializer.Deserialize<Receipt>(element.GetRawText())
            ?? throw new JsonException("receipt was null");
    }
}
