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
