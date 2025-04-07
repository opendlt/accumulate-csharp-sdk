using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Rpc.Models
{
    /// <summary>
    /// Represents an error in a JSON-RPC 2.0 response.
    /// </summary>
    public class RPCError
    {
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
        /// Gets or sets additional error data.
        /// </summary>
        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }

        /// <summary>
        /// Returns a string representation of the error.
        /// </summary>
        /// <returns>A string describing the error.</returns>
        public override string ToString()
        {
            return $"RPC Error {Code}: {Message ?? "Unknown error"}";
        }
    }
} 