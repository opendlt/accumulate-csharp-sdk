using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents a token response from the Acme API.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the token type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token URL.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token's issuer URL.
        /// </summary>
        [JsonPropertyName("issuerUrl")]
        public string IssuerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token's symbol.
        /// </summary>
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token's precision.
        /// </summary>
        [JsonPropertyName("precision")]
        public int Precision { get; set; }

        /// <summary>
        /// Gets or sets the token's supply.
        /// </summary>
        [JsonPropertyName("supply")]
        public long Supply { get; set; }

        /// <summary>
        /// Gets or sets the token's issued amount.
        /// </summary>
        [JsonPropertyName("issued")]
        public long Issued { get; set; }
    }

    /// <summary>
    /// Represents a list of tokens response from the Acme API.
    /// </summary>
    public class TokensResponse
    {
        /// <summary>
        /// Gets or sets the list of tokens.
        /// </summary>
        [JsonPropertyName("tokens")]
        public List<TokenResponse> Tokens { get; set; } = new List<TokenResponse>();

        /// <summary>
        /// Gets or sets the total number of tokens.
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the start index for pagination.
        /// </summary>
        [JsonPropertyName("start")]
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the count of tokens returned.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
} 