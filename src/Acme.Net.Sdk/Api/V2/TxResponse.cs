using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents a transaction response from the API.
    /// This is a placeholder implementation that will be expanded later.
    /// </summary>
    public class TxResponse
    {
        /// <summary>
        /// Gets or sets the transaction ID.
        /// </summary>
        [JsonPropertyName("txid")]
        public string? TxId { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the result of the transaction.
        /// </summary>
        [JsonPropertyName("result")]
        public JsonElement? Result { get; set; }
    }
} 