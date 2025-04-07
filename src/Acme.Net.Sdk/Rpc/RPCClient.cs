using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Rpc.Models;

namespace Acme.Net.Sdk.Rpc
{
    /// <summary>
    /// Abstract base class for RPC clients that communicate with the Acme network.
    /// Corresponds to io.accumulatenetwork.sdk.rpc.RPCClient.
    /// </summary>
    public abstract class RPCClient
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);
        private static readonly Random Random = new Random();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly Uri _uri;
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCClient"/> class with an API endpoint from environment variables.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the API endpoint is not configured.</exception>
        protected RPCClient()
        {
            string? apiEndpoint = Environment.GetEnvironmentVariable("ACC_API");
            if (string.IsNullOrEmpty(apiEndpoint))
            {
                apiEndpoint = Environment.GetEnvironmentVariable("ACCUMULATE_API");
            }

            if (string.IsNullOrEmpty(apiEndpoint))
            {
                throw new InvalidOperationException(
                    "The RPCClient() constructor needs environment variable ACC_API or ACCUMULATE_API " +
                    "containing the Accumulate API endpoint");
            }

            _uri = new Uri(apiEndpoint);
            _httpClient = new HttpClient
            {
                Timeout = DefaultTimeout
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCClient"/> class with a specified API endpoint.
        /// </summary>
        /// <param name="uri">The URI of the API endpoint.</param>
        protected RPCClient(Uri uri)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _httpClient = new HttpClient
            {
                Timeout = DefaultTimeout
            };
        }

        /// <summary>
        /// Builds an HTTP request for an RPC method call.
        /// </summary>
        /// <param name="requestId">The request ID.</param>
        /// <param name="rpcMethod">The RPC method to call.</param>
        /// <param name="body">The request body.</param>
        /// <returns>The HTTP request message.</returns>
        protected HttpRequestMessage BuildRequest(int? requestId, Rpc.Models.RPCMethod rpcMethod, object? body)
        {
            // Convert the body to a JsonElement if it exists
            JsonElement? jsonParams = null;
            if (body != null)
            {
                string bodyJson = JsonSerializer.Serialize(body, JsonOptions);
                jsonParams = JsonDocument.Parse(bodyJson).RootElement;
            }

            var rpcRequest = new RPCRequest("2.0", requestId, rpcMethod.ApiMethod, jsonParams);
            string requestJson = JsonSerializer.Serialize(rpcRequest, JsonOptions);
            
            // Log the request if needed
            // If using a logging framework, we would log the requestJson here

            var request = new HttpRequestMessage(HttpMethod.Post, _uri)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            return request;
        }

        /// <summary>
        /// Creates an exception for a failed HTTP request.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <returns>A RuntimeException wrapping the original exception.</returns>
        protected Exception BuildRequestException(Exception exception)
        {
            return new InvalidOperationException($"Posting the request to Accumulate endpoint {_uri} failed", exception);
        }

        /// <summary>
        /// Creates an exception for an error HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>A RuntimeException describing the error.</returns>
        protected Exception BuildResponseException(HttpResponseMessage response)
        {
            string content = response.Content.ReadAsStringAsync().Result;
            return new InvalidOperationException(
                $"HTTP error response from Accumulate endpoint {_uri}, status code: {(int)response.StatusCode}, message: {content}");
        }

        /// <summary>
        /// Generates a new random request ID.
        /// </summary>
        /// <returns>A random integer between 0 and 4999.</returns>
        protected int NewRequestId()
        {
            return Random.Next(5000); // Same limit as Java implementation
        }

        /// <summary>
        /// Sends an RPC request to the API endpoint and returns the response.
        /// </summary>
        /// <param name="rpcMethod">The RPC method to call.</param>
        /// <param name="body">The request body.</param>
        /// <returns>The RPC response.</returns>
        /// <exception cref="RPCException">Thrown if the API returns an error.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the HTTP request fails.</exception>
        protected virtual RPCResponse SendInternalSync(Rpc.Models.RPCMethod rpcMethod, object? body)
        {
            try
            {
                int requestId = NewRequestId();
                var request = BuildRequest(requestId, rpcMethod, body);
                var response = _httpClient.Send(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw BuildResponseException(response);
                }

                string responseContent = response.Content.ReadAsStringAsync().Result;
                
                // Log the response if needed
                // If using a logging framework, we would log the responseContent here

                return RPCResponse.From(responseContent);
            }
            catch (RPCException)
            {
                throw; // Rethrow RPC exceptions as they are
            }
            catch (Exception ex)
            {
                throw BuildRequestException(ex);
            }
        }
    }
} 