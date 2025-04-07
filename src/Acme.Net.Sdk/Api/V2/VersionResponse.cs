using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents a version information response from the Acme API.
    /// </summary>
    public class VersionResponse
    {
        /// <summary>
        /// Gets or sets the version string.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the commit hash.
        /// </summary>
        [JsonPropertyName("commit")]
        public string Commit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the build time.
        /// </summary>
        [JsonPropertyName("buildTime")]
        public string BuildTime { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the network name.
        /// </summary>
        [JsonPropertyName("network")]
        public string Network { get; set; } = string.Empty;
    }
} 