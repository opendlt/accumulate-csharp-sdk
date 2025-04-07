using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents a partition information response from the Acme API.
    /// </summary>
    public class PartitionResponse
    {
        /// <summary>
        /// Gets or sets the partition ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the partition type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the partition's anchor ledger state.
        /// </summary>
        [JsonPropertyName("anchorState")]
        public AnchorState? AnchorState { get; set; }

        /// <summary>
        /// Gets or sets the validators for this partition.
        /// </summary>
        [JsonPropertyName("validators")]
        public List<ValidatorInfo> Validators { get; set; } = new List<ValidatorInfo>();
    }

    /// <summary>
    /// Represents a list of partitions in the network.
    /// </summary>
    public class PartitionsResponse
    {
        /// <summary>
        /// Gets or sets the list of partition IDs.
        /// </summary>
        [JsonPropertyName("ids")]
        public List<string> Ids { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents information about a partition in the network.
    /// </summary>
    public class PartitionInfo
    {
        /// <summary>
        /// Gets or sets the partition ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the partition type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents anchor state information for a partition.
    /// </summary>
    public class AnchorState
    {
        /// <summary>
        /// Gets or sets the anchor's root chain height.
        /// </summary>
        [JsonPropertyName("rootHeight")]
        public long RootHeight { get; set; }

        /// <summary>
        /// Gets or sets the anchor's major block height.
        /// </summary>
        [JsonPropertyName("majorHeight")]
        public long MajorHeight { get; set; }

        /// <summary>
        /// Gets or sets the anchor's minor block height.
        /// </summary>
        [JsonPropertyName("minorHeight")]
        public long MinorHeight { get; set; }
    }

    /// <summary>
    /// Represents information about a validator in the network.
    /// </summary>
    public class ValidatorInfo
    {
        /// <summary>
        /// Gets or sets the validator's public key.
        /// </summary>
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validator's address.
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
    }
} 