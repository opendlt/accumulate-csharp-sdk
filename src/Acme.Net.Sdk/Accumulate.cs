using System.Text.Json;
using Acme.Net.Sdk.Core;
using Acme.Net.Sdk.V2;
using Acme.Net.Sdk.V3;

namespace Acme.Net.Sdk
{
    /// <summary>
    /// Unified entry point for the Accumulate SDK.
    /// Provides access to both V2 and V3 JSON-RPC clients and convenience methods.
    /// Matches the Python SDK's Accumulate class and Dart's Accumulate facade.
    /// </summary>
    public class Accumulate : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// The V2 JSON-RPC client.
        /// </summary>
        public AccumulateV2Client V2 { get; }

        /// <summary>
        /// The V3 JSON-RPC client.
        /// </summary>
        public AccumulateV3Client V3 { get; }

        /// <summary>
        /// The base endpoint URL.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Creates a new Accumulate client for the given base endpoint.
        /// Automatically creates both V2 (/v2) and V3 (/v3) sub-clients.
        /// </summary>
        /// <param name="endpoint">The base endpoint URL (e.g., "https://testnet.accumulatenetwork.io").</param>
        /// <param name="timeout">HTTP request timeout for both clients.</param>
        /// <param name="httpClient">Optional shared HttpClient for both clients.</param>
        public Accumulate(string endpoint, TimeSpan? timeout = null, HttpClient? httpClient = null)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            var v2Url = NetworkEndpoints.GetV2Url(endpoint);
            var v3Url = NetworkEndpoints.GetV3Url(endpoint);

            V2 = new AccumulateV2Client(v2Url, timeout, httpClient);
            V3 = new AccumulateV3Client(v3Url, timeout, httpClient);
        }

        // ---- Factory methods ----

        /// <summary>
        /// Creates a client connected to the Accumulate mainnet.
        /// </summary>
        public static Accumulate Mainnet(TimeSpan? timeout = null)
        {
            return new Accumulate(NetworkEndpoints.MainnetBaseUrl, timeout);
        }

        /// <summary>
        /// Creates a client connected to the Accumulate testnet.
        /// </summary>
        public static Accumulate Testnet(TimeSpan? timeout = null)
        {
            return new Accumulate(NetworkEndpoints.TestnetBaseUrl, timeout);
        }

        /// <summary>
        /// Creates a client connected to a local devnet instance.
        /// </summary>
        public static Accumulate Devnet(string host = "localhost", int port = 26660, TimeSpan? timeout = null)
        {
            var baseUrl = NetworkEndpoints.GetDevnetBaseUrl(host, port);
            return new Accumulate(baseUrl, timeout);
        }

        /// <summary>
        /// Creates a client connected to localhost on the given port.
        /// </summary>
        public static Accumulate Local(int port = 26660, TimeSpan? timeout = null)
        {
            return Devnet("localhost", port, timeout);
        }

        // ---- V3 convenience methods ----

        /// <summary>
        /// Submits an envelope to the network via V3.
        /// </summary>
        public Task<List<JsonElement>> SubmitAsync(object envelope)
        {
            return V3.SubmitAsync(envelope);
        }

        /// <summary>
        /// General-purpose query via V3.
        /// </summary>
        public Task<JsonElement> QueryAsync(string scope, object? query = null)
        {
            return V3.QueryAsync(scope, query);
        }

        /// <summary>
        /// Requests tokens from the faucet via V3.
        /// </summary>
        public Task<JsonElement> FaucetAsync(string accountUrl)
        {
            return V3.FaucetAsync(accountUrl);
        }

        /// <summary>
        /// Queries an account by URL via V3.
        /// </summary>
        public Task<JsonElement> QueryAccountAsync(string url)
        {
            return V3.QueryAccountAsync(url);
        }

        /// <summary>
        /// Queries a transaction by ID via V3.
        /// </summary>
        public Task<JsonElement> QueryTransactionAsync(string txid)
        {
            return V3.QueryTransactionAsync(txid);
        }

        // ---- V2 convenience methods ----

        /// <summary>
        /// Executes a transaction envelope directly via V2.
        /// </summary>
        public Task<JsonElement> ExecuteDirectAsync(object envelope)
        {
            return V2.ExecuteDirectAsync(envelope);
        }

        // ---- IDisposable ----

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                V2.Dispose();
                V3.Dispose();
                _disposed = true;
            }
        }
    }
}
