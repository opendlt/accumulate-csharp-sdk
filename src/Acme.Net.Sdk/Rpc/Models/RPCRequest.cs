using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Rpc.Models
{
    /// <summary>
    /// Represents a request to a JSON-RPC 2.0 API.
    /// </summary>
    public class RPCRequest
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version. Always "2.0".
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; }

        /// <summary>
        /// Gets or sets the ID of the request.
        /// </summary>
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the method name to call.
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the method call.
        /// </summary>
        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCRequest"/> class.
        /// </summary>
        /// <param name="jsonRpc">The JSON-RPC version.</param>
        /// <param name="id">The request ID.</param>
        /// <param name="method">The method name.</param>
        /// <param name="params">The method parameters.</param>
        public RPCRequest(string jsonRpc, int? id, string method, JsonElement? @params)
        {
            JsonRpc = jsonRpc;
            Id = id;
            Method = method;
            Params = @params;
        }
    }
} 