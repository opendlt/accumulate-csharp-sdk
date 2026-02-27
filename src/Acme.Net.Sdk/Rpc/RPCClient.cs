using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; // ← needed for JsonIgnoreCondition
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

        // Global JSON options used for ALL outbound JSON (requests and params)
        // - camelCase
        // - pretty for logs
        // - OMIT NULLS so we never send "memo": null, etc.
        protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            WriteIndented          = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        // ====== simple pluggable logger ======
        public static Action<string>? LogSink { get; set; }
        protected static void Log(string msg) => LogSink?.Invoke(msg);
        // =====================================

        private readonly Uri _uri;
        protected readonly HttpClient _httpClient;

        protected RPCClient()
        {
            string? apiEndpoint = Environment.GetEnvironmentVariable("ACC_API");
            if (string.IsNullOrEmpty(apiEndpoint))
                apiEndpoint = Environment.GetEnvironmentVariable("ACCUMULATE_API");

            if (string.IsNullOrEmpty(apiEndpoint))
                throw new InvalidOperationException(
                    "The RPCClient() constructor needs environment variable ACC_API or ACCUMULATE_API containing the Accumulate API endpoint");

            _uri = new Uri(apiEndpoint);
            _httpClient = new HttpClient { Timeout = DefaultTimeout };
        }

        protected RPCClient(Uri uri)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _httpClient = new HttpClient { Timeout = DefaultTimeout };
        }

        /// <summary>Build an HTTP request for a JSON-RPC call.</summary>
        protected HttpRequestMessage BuildRequest(int? requestId, Rpc.Models.RPCMethod rpcMethod, object? body)
        {
            JsonElement? jsonParams = null;

            if (body != null)
            {
                // Serialize with global options (nulls omitted), then parse to JsonElement
                string bodyJson = JsonSerializer.Serialize(body, JsonOptions);
                jsonParams = JsonDocument.Parse(bodyJson).RootElement;
            }

            var rpcRequest = new RPCRequest("2.0", requestId, rpcMethod.ApiMethod, jsonParams);
            string requestJson = JsonSerializer.Serialize(rpcRequest, JsonOptions);

            Log($"--- RPC REQUEST ({rpcMethod.ApiMethod}) ---> {_uri}\n{requestJson}\n--- END REQUEST ---");

            return new HttpRequestMessage(HttpMethod.Post, _uri)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
        }

        protected Exception BuildRequestException(Exception exception)
        {
            Log($"*** RPC REQUEST ERROR ***\nEndpoint: {_uri}\n{exception}\n*** END REQUEST ERROR ***");
            return new InvalidOperationException($"Posting the request to Accumulate endpoint {_uri} failed", exception);
        }

        protected Exception BuildResponseException(HttpResponseMessage response)
        {
            string content = response.Content.ReadAsStringAsync().Result;
            Log($"*** RPC HTTP ERROR ***\nEndpoint: {_uri}\nStatus: {(int)response.StatusCode} {response.StatusCode}\nBody:\n{content}\n*** END HTTP ERROR ***");
            return new InvalidOperationException(
                $"HTTP error response from Accumulate endpoint {_uri}, status code: {(int)response.StatusCode}, message: {content}");
        }

        protected int NewRequestId() => Random.Next(5000);

        protected virtual RPCResponse SendInternalSync(Rpc.Models.RPCMethod rpcMethod, object? body)
        {
            try
            {
                int requestId = NewRequestId();
                var request   = BuildRequest(requestId, rpcMethod, body);
                var response  = _httpClient.Send(request);

                if (!response.IsSuccessStatusCode)
                    throw BuildResponseException(response);

                string responseContent = response.Content.ReadAsStringAsync().Result;
                Log($"--- RPC RESPONSE ({rpcMethod.ApiMethod}) <--- {_uri} (HTTP {(int)response.StatusCode})\n{responseContent}\n--- END RESPONSE ---");

                return RPCResponse.From(responseContent);
            }
            catch (RPCException) { throw; }
            catch (Exception ex) { throw BuildRequestException(ex); }
        }
    }
}
