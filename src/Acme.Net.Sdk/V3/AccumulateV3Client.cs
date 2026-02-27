using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Net.Sdk.Exceptions;

namespace Acme.Net.Sdk.V3
{
    /// <summary>
    /// V3 JSON-RPC client for the Accumulate network.
    /// Matches the Python SDK's AccumulateV3Client class.
    /// All methods are async and return JsonElement for maximum flexibility.
    /// </summary>
    public class AccumulateV3Client : IDisposable
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
        /// Creates a new V3 client for the given endpoint.
        /// </summary>
        /// <param name="endpoint">The V3 JSON-RPC endpoint URL (e.g., "https://testnet.accumulatenetwork.io/v3").</param>
        /// <param name="timeout">HTTP request timeout. Defaults to 90 seconds.</param>
        /// <param name="httpClient">Optional shared HttpClient. If not provided, one will be created and owned by this instance.</param>
        public AccumulateV3Client(string endpoint, TimeSpan? timeout = null, HttpClient? httpClient = null)
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

        // ---- Low-level ----

        /// <summary>
        /// Sends a raw JSON-RPC 2.0 call to the V3 endpoint.
        /// </summary>
        public async Task<JsonElement> CallAsync(string method, object? parameters = null)
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
                // Serialize to JsonElement to ensure proper nesting
                var paramsJson = JsonSerializer.Serialize(parameters, JsonOptions);
                request["params"] = JsonDocument.Parse(paramsJson).RootElement;
            }
            else
            {
                // V3 API requires params to be present, even as empty object
                request["params"] = JsonDocument.Parse("{}").RootElement;
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

            // Check for JSON-RPC error
            if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
            {
                var code = errorElement.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
                var message = errorElement.TryGetProperty("message", out var m) ? m.GetString() ?? "Unknown error" : "Unknown error";
                JsonElement? data = errorElement.TryGetProperty("data", out var d) ? d.Clone() : null;
                throw new AccumulateApiException(code, message, data);
            }

            // Return the result
            if (root.TryGetProperty("result", out var result))
                return result.Clone();

            // Some V3 methods return the whole response as the result
            return root.Clone();
        }

        // ---- Submission ----

        /// <summary>
        /// Submits an envelope to the network.
        /// </summary>
        public async Task<List<JsonElement>> SubmitAsync(object envelope)
        {
            var wrapped = new Dictionary<string, object?> { ["envelope"] = envelope };
            var result = await CallAsync("submit", wrapped).ConfigureAwait(false);
            return ExtractResultList(result);
        }

        /// <summary>
        /// Validates an envelope without submitting it (dry-run).
        /// </summary>
        public async Task<List<JsonElement>> ValidateAsync(object envelope)
        {
            var wrapped = new Dictionary<string, object?> { ["envelope"] = envelope };
            var result = await CallAsync("validate", wrapped).ConfigureAwait(false);
            return ExtractResultList(result);
        }

        // ---- Faucet ----

        /// <summary>
        /// Requests tokens from the faucet for the given account URL.
        /// </summary>
        public Task<JsonElement> FaucetAsync(string accountUrl)
        {
            return CallAsync("faucet", new { account = accountUrl });
        }

        // ---- Queries ----

        /// <summary>
        /// General-purpose query by scope URL.
        /// </summary>
        public Task<JsonElement> QueryAsync(string scope, object? query = null)
        {
            var parameters = new Dictionary<string, object?> { ["scope"] = scope };
            if (query != null)
            {
                parameters["query"] = query;
            }
            return CallAsync("query", parameters);
        }

        /// <summary>
        /// Queries an account by URL.
        /// </summary>
        public Task<JsonElement> QueryAccountAsync(string url)
        {
            return QueryAsync(url);
        }

        /// <summary>
        /// Queries a transaction by transaction ID.
        /// </summary>
        public Task<JsonElement> QueryTransactionAsync(string txid)
        {
            return QueryAsync(txid);
        }

        /// <summary>
        /// Queries a chain on an account.
        /// </summary>
        public Task<JsonElement> QueryChainAsync(string url, string chainName, RangeOptions? range = null)
        {
            var query = new Dictionary<string, object?> { ["queryType"] = "chain", ["name"] = chainName };
            if (range != null) query["range"] = BuildRangeDict(range);
            return QueryAsync(url, query);
        }

        /// <summary>
        /// Queries data entries on a data account.
        /// </summary>
        public Task<JsonElement> QueryDataAsync(string url, int? index = null, string? entryHash = null, RangeOptions? range = null)
        {
            var query = new Dictionary<string, object?> { ["queryType"] = "data" };
            if (index.HasValue) query["index"] = index.Value;
            if (entryHash != null) query["entry"] = entryHash;
            if (range != null) query["range"] = BuildRangeDict(range);
            return QueryAsync(url, query);
        }

        /// <summary>
        /// Queries the directory of an identity or key book.
        /// </summary>
        public Task<JsonElement> QueryDirectoryAsync(string url, RangeOptions? range = null)
        {
            var query = new Dictionary<string, object?> { ["queryType"] = "directory" };
            if (range != null) query["range"] = BuildRangeDict(range);
            return QueryAsync(url, query);
        }

        /// <summary>
        /// Queries pending transactions for an account.
        /// </summary>
        public Task<JsonElement> QueryPendingAsync(string url, RangeOptions? range = null)
        {
            var query = new Dictionary<string, object?> { ["queryType"] = "pending" };
            if (range != null) query["range"] = BuildRangeDict(range);
            return QueryAsync(url, query);
        }

        /// <summary>
        /// Queries minor blocks for a partition or network URL.
        /// </summary>
        public Task<JsonElement> QueryMinorBlocksAsync(string url, RangeOptions? range = null)
        {
            var query = new Dictionary<string, object?> { ["queryType"] = "block" };
            if (range != null) query["minorRange"] = BuildRangeDict(range);
            return QueryAsync(url, query);
        }

        /// <summary>
        /// Queries major blocks for a partition or network URL.
        /// </summary>
        public Task<JsonElement> QueryMajorBlocksAsync(string url, RangeOptions? range = null)
        {
            var query = new Dictionary<string, object?> { ["queryType"] = "block" };
            if (range != null) query["majorRange"] = BuildRangeDict(range);
            return QueryAsync(url, query);
        }

        private static Dictionary<string, object> BuildRangeDict(RangeOptions range)
        {
            var dict = new Dictionary<string, object>();
            if (range.Start.HasValue) dict["start"] = range.Start.Value;
            if (range.Count.HasValue) dict["count"] = range.Count.Value;
            if (range.Expand.HasValue) dict["expand"] = range.Expand.Value;
            return dict;
        }

        // ---- Search ----

        /// <summary>
        /// Searches for an anchor by hash.
        /// </summary>
        public Task<JsonElement> SearchAnchorAsync(string url, string anchor)
        {
            return QueryAsync(url, new { queryType = "anchor", anchor });
        }

        /// <summary>
        /// Searches for a public key in a key book/page.
        /// </summary>
        public Task<JsonElement> SearchPublicKeyAsync(string url, string publicKey, string signatureType = "ed25519")
        {
            return QueryAsync(url, new { queryType = "publicKey", publicKey, type = signatureType });
        }

        /// <summary>
        /// Searches for a public key hash in a key book/page.
        /// </summary>
        public Task<JsonElement> SearchPublicKeyHashAsync(string url, string keyHash)
        {
            return QueryAsync(url, new { queryType = "publicKeyHash", publicKeyHash = keyHash });
        }

        /// <summary>
        /// Searches for a delegate in an authority.
        /// </summary>
        public Task<JsonElement> SearchDelegateAsync(string url, string delegateUrl)
        {
            return QueryAsync(url, new { queryType = "delegate", @delegate = delegateUrl });
        }

        // ---- Node / Network ----

        /// <summary>
        /// Gets information about the connected node.
        /// </summary>
        public Task<JsonElement> NodeInfoAsync()
        {
            return CallAsync("node-info");
        }

        /// <summary>
        /// Finds services available on the network.
        /// </summary>
        public Task<JsonElement> FindServiceAsync(object? options = null)
        {
            return CallAsync("find-service", options);
        }

        /// <summary>
        /// Gets the consensus status of a partition.
        /// </summary>
        public Task<JsonElement> ConsensusStatusAsync(object options)
        {
            return CallAsync("consensus-status", options);
        }

        /// <summary>
        /// Gets the network status.
        /// </summary>
        public Task<JsonElement> NetworkStatusAsync(object options)
        {
            return CallAsync("network-status", options);
        }

        /// <summary>
        /// Gets metrics from the node.
        /// </summary>
        public Task<JsonElement> MetricsAsync(object? options = null)
        {
            return CallAsync("metrics", options);
        }

        // ---- Helpers ----

        private static List<JsonElement> ExtractResultList(JsonElement result)
        {
            var list = new List<JsonElement>();
            if (result.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.EnumerateArray())
                    list.Add(item.Clone());
            }
            else
            {
                list.Add(result);
            }
            return list;
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
