using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.V3
{
    /// <summary>
    /// Options for range-based queries (chains, directories, blocks, etc.).
    /// </summary>
    public class RangeOptions
    {
        [JsonPropertyName("start")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Start { get; set; }

        [JsonPropertyName("count")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Count { get; set; }

        [JsonPropertyName("expand")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Expand { get; set; }
    }

    /// <summary>
    /// Options for transaction submission.
    /// </summary>
    public class SubmitOptions
    {
        /// <summary>
        /// If true, wait for the transaction to be accepted before returning.
        /// </summary>
        [JsonPropertyName("wait")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Wait { get; set; }

        /// <summary>
        /// If true, verify the transaction was delivered successfully.
        /// </summary>
        [JsonPropertyName("verify")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Verify { get; set; }
    }

    /// <summary>
    /// Options for transaction validation (dry-run).
    /// </summary>
    public class ValidateOptions
    {
        /// <summary>
        /// If true, perform a full validation including signature checks.
        /// </summary>
        [JsonPropertyName("full")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Full { get; set; }
    }
}

namespace Acme.Net.Sdk.V3
{
    /// <summary>
    /// Receipt options for queries (<c>includeReceipt</c>).
    /// </summary>
    public class ReceiptOptions
    {
        [JsonPropertyName("forAny")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ForAny { get; set; }

        /// <summary>Request a receipt that reaches at least this block height (Accumulate 1.4.6.7).</summary>
        [JsonPropertyName("forHeight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ulong ForHeight { get; set; }
    }

    /// <summary>
    /// Options for <c>major-header-range</c>: a record per major block in [Start, End]. Directory only.
    /// </summary>
    public class MajorHeaderRangeOptions
    {
        [JsonPropertyName("partition")]
        public string Partition { get; set; } = "";

        [JsonPropertyName("start")]
        public ulong Start { get; set; }

        /// <summary>Last major block index, inclusive.</summary>
        [JsonPropertyName("end")]
        public ulong End { get; set; }
    }

    /// <summary>
    /// Options for <c>minor-root-range</c>. Directory only.
    /// </summary>
    public class MinorRootRangeOptions
    {
        [JsonPropertyName("partition")]
        public string Partition { get; set; } = "";

        /// <summary>The client's last verified minor block.</summary>
        [JsonPropertyName("since")]
        public ulong Since { get; set; }

        /// <summary>The target minor block, or zero for as far as possible.</summary>
        [JsonPropertyName("until")]
        public ulong Until { get; set; }
    }

    /// <summary>
    /// Options for <c>anchor-receipt</c>: binds a partition's BPT root to a directory root.
    /// </summary>
    public class AnchorReceiptOptions
    {
        /// <summary>The partition whose BPT root is being bound (from the first call's Receipt.Partition).</summary>
        [JsonPropertyName("partition")]
        public string Partition { get; set; } = "";

        /// <summary>Hex of the BPT root where the first call's receipt terminates.</summary>
        [JsonPropertyName("bptRoot")]
        public string BptRoot { get; set; } = "";

        /// <summary>Optional: a receipt terminating at a directory root no older than this block (0 = oldest that works).</summary>
        [JsonPropertyName("atOrAfter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ulong AtOrAfter { get; set; }
    }
}
