using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Net.Sdk.Exceptions;

namespace Acme.Net.Sdk.V2
{
    /// <summary>
    /// V2 JSON-RPC client for the Accumulate network.
    /// Thin wrapper providing a clean, named API matching Python's AccumulateV2Client.
    /// Uses direct HTTP calls to the V2 endpoint for simplicity.
    /// </summary>
    public class AccumulateV2Client : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly string _endpoint;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private int _requestId;
        private bool _disposed;

        /// <summary>
        /// Creates a new V2 client for the given endpoint.
        /// </summary>
        /// <param name="endpoint">The V2 JSON-RPC endpoint URL (e.g., "https://testnet.accumulatenetwork.io/v2").</param>
        /// <param name="timeout">HTTP request timeout. Defaults to 90 seconds.</param>
        /// <param name="httpClient">Optional shared HttpClient. If not provided, one will be created and owned by this instance.</param>
        public AccumulateV2Client(string endpoint, TimeSpan? timeout = null, HttpClient? httpClient = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else
            {
                _httpClient = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(90) };
                _ownsHttpClient = true;
            }
        }

        /// <summary>
        /// Sends a raw JSON-RPC 2.0 call to the V2 endpoint.
        /// </summary>
        private async Task<JsonElement> CallAsync(string method, object? parameters = null)
        {
            var requestId = Interlocked.Increment(ref _requestId);

            var request = new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = requestId,
                ["method"] = method,
            };

            if (parameters != null)
            {
                var paramsJson = JsonSerializer.Serialize(parameters, JsonOptions);
                request["params"] = JsonDocument.Parse(paramsJson).RootElement;
            }

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(_endpoint, content).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new AccumulateNetworkException($"Failed to connect to {_endpoint}: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new AccumulateNetworkException($"Request to {_endpoint} timed out", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new AccumulateNetworkException(
                    $"HTTP {(int)response.StatusCode} from {_endpoint}: {body}",
                    statusCode: (int)response.StatusCode);
            }

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
            {
                var code = errorElement.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
                var message = errorElement.TryGetProperty("message", out var m) ? m.GetString() ?? "Unknown error" : "Unknown error";
                JsonElement? data = errorElement.TryGetProperty("data", out var d) ? d.Clone() : null;
                throw new AccumulateApiException(code, message, data);
            }

            if (root.TryGetProperty("result", out var result))
                return result.Clone();

            return root.Clone();
        }

        // ---- Transaction Execution ----

        /// <summary>
        /// Executes a transaction envelope via the V2 "execute" method.
        /// </summary>
        public Task<JsonElement> ExecuteAsync(object envelope)
        {
            return CallAsync("execute", envelope);
        }

        /// <summary>
        /// Executes a transaction envelope directly via the V2 "execute-direct" method.
        /// </summary>
        public Task<JsonElement> ExecuteDirectAsync(object envelope)
        {
            return CallAsync("execute-direct", envelope);
        }

        // ---- Queries ----

        /// <summary>
        /// Queries an account or resource by URL.
        /// </summary>
        public Task<JsonElement> QueryAsync(string url)
        {
            return CallAsync("query", new { url });
        }

        /// <summary>
        /// Queries a transaction by its transaction ID.
        /// </summary>
        public Task<JsonElement> QueryTxAsync(string txid)
        {
            return CallAsync("query-tx", new { txid });
        }

        /// <summary>
        /// Queries the directory of an identity.
        /// </summary>
        public Task<JsonElement> QueryDirectoryAsync(string url, int start = 0, int? count = null)
        {
            var parameters = new Dictionary<string, object?> { ["url"] = url, ["start"] = start };
            if (count.HasValue) parameters["count"] = count.Value;
            return CallAsync("query-directory", parameters);
        }

        /// <summary>
        /// Queries data entries on a data account.
        /// </summary>
        public Task<JsonElement> QueryDataAsync(string url, string? entryHash = null)
        {
            var parameters = new Dictionary<string, object?> { ["url"] = url };
            if (entryHash != null) parameters["entryHash"] = entryHash;
            return CallAsync("query-data", parameters);
        }

        /// <summary>
        /// Queries the transaction history for an account.
        /// </summary>
        public Task<JsonElement> QueryTxHistoryAsync(string url, int start = 0, int? count = null)
        {
            var parameters = new Dictionary<string, object?> { ["url"] = url, ["start"] = start };
            if (count.HasValue) parameters["count"] = count.Value;
            return CallAsync("query-tx-history", parameters);
        }

        // ---- Faucet ----

        /// <summary>
        /// Requests tokens from the faucet for the given URL.
        /// </summary>
        public Task<JsonElement> FaucetAsync(string url)
        {
            return CallAsync("faucet", new { url });
        }

        // ---- Status ----

        /// <summary>
        /// Gets the network status.
        /// </summary>
        public Task<JsonElement> StatusAsync()
        {
            return CallAsync("status");
        }

        /// <summary>
        /// Gets the node version information.
        /// </summary>
        public Task<JsonElement> VersionAsync()
        {
            return CallAsync("version");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_ownsHttpClient)
                    _httpClient.Dispose();
                _disposed = true;
            }
        }
    }
}
