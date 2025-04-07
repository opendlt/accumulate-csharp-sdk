using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Net.Sdk.Api.V2;

namespace Acme.Net.Sdk.Rpc.Models
{
    /// <summary>
    /// Represents a response from a JSON-RPC 2.0 API call.
    /// </summary>
    public class RPCResponse
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets the ID of the request this response corresponds to.
        /// </summary>
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the result of the RPC call, if successful.
        /// </summary>
        [JsonPropertyName("result")]
        public JsonElement? Result { get; set; }

        /// <summary>
        /// Gets or sets the error information, if the call failed.
        /// </summary>
        [JsonPropertyName("error")]
        public RPCError? Error { get; set; }

        /// <summary>
        /// Creates an RPCResponse instance from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>The parsed RPCResponse.</returns>
        /// <exception cref="RPCException">Thrown if the response contains an error.</exception>
        public static RPCResponse From(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var response = JsonSerializer.Deserialize<RPCResponse>(json, options);
            
            if (response == null)
            {
                throw new RPCException("Failed to parse RPC response");
            }
            
            if (response.Error != null)
            {
                throw new RPCException(response.Error);
            }
            
            return response;
        }

        /// <summary>
        /// Converts this response to a TxResponse.
        /// </summary>
        /// <returns>The transaction response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the response does not contain a valid result.</exception>
        public TxResponse AsTransactionResponse()
        {
            if (Result == null)
            {
                throw new InvalidOperationException("RPC response does not contain a result");
            }
            
            string resultJson = Result.Value.GetRawText();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var txResponse = JsonSerializer.Deserialize<TxResponse>(resultJson, options);
            
            if (txResponse == null)
            {
                throw new InvalidOperationException("Failed to parse transaction response");
            }
            
            return txResponse;
        }
    }
} 