using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents a network status response from the Acme API.
    /// </summary>
    public class NetworkStatusResponse
    {
        /// <summary>
        /// Gets or sets the network type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the node.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the network name.
        /// </summary>
        [JsonPropertyName("network")]
        public string Network { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the oracle price of ACME in USD.
        /// </summary>
        [JsonPropertyName("oracle")]
        public decimal? OraclePriceUsd { get; set; }

        /// <summary>
        /// Gets or sets the list of partition information.
        /// </summary>
        [JsonPropertyName("partitions")]
        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();

        /// <summary>
        /// Gets or sets the routing table information.
        /// </summary>
        [JsonPropertyName("routing")]
        public RoutingInfo? Routing { get; set; }
    }

    /// <summary>
    /// Represents routing information for the network.
    /// </summary>
    public class RoutingInfo
    {
        /// <summary>
        /// Gets or sets the routing version.
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the routing table.
        /// </summary>
        [JsonPropertyName("table")]
        public Dictionary<string, string> Table { get; set; } = new Dictionary<string, string>();
    }
} 