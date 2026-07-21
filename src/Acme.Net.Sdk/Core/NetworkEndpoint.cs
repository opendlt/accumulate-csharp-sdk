namespace Acme.Net.Sdk.Core
{
    /// <summary>
    /// Well-known Accumulate network endpoints.
    /// </summary>
    public enum NetworkEndpoint
    {
        Mainnet,
        Testnet,
        Devnet
    }

    /// <summary>
    /// Provides base URLs and URL-building helpers for Accumulate network endpoints.
    /// Matches the Dart SDK's endpoints and Python SDK's constants.
    /// </summary>
    public static class NetworkEndpoints
    {
        public const string MainnetBaseUrl = "https://mainnet.accumulatenetwork.io";
        public const string TestnetBaseUrl = "https://testnet.accumulatenetwork.io";
        public const string KermitBaseUrl = "https://kermit.accumulatenetwork.io";
        public const string DevnetBaseUrl = "http://localhost:26660";
        public const int DefaultDevnetPort = 26660;

        /// <summary>
        /// Gets the V2 JSON-RPC URL for a well-known network endpoint.
        /// </summary>
        public static string GetV2Url(NetworkEndpoint endpoint)
        {
            return GetV2Url(GetBaseUrl(endpoint));
        }

        /// <summary>
        /// Gets the V3 JSON-RPC URL for a well-known network endpoint.
        /// </summary>
        public static string GetV3Url(NetworkEndpoint endpoint)
        {
            return GetV3Url(GetBaseUrl(endpoint));
        }

        /// <summary>
        /// Appends "/v2" to a base URL.
        /// </summary>
        public static string GetV2Url(string baseUrl)
        {
            return baseUrl.TrimEnd('/') + "/v2";
        }

        /// <summary>
        /// Appends "/v3" to a base URL.
        /// </summary>
        public static string GetV3Url(string baseUrl)
        {
            return baseUrl.TrimEnd('/') + "/v3";
        }

        /// <summary>
        /// Gets the base URL for a well-known network endpoint.
        /// </summary>
        public static string GetBaseUrl(NetworkEndpoint endpoint)
        {
            return endpoint switch
            {
                NetworkEndpoint.Mainnet => MainnetBaseUrl,
                NetworkEndpoint.Testnet => TestnetBaseUrl,
                NetworkEndpoint.Devnet => DevnetBaseUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unknown network endpoint")
            };
        }

        /// <summary>
        /// Builds a devnet base URL from host and port.
        /// </summary>
        public static string GetDevnetBaseUrl(string host = "localhost", int port = DefaultDevnetPort)
        {
            return $"http://{host}:{port}";
        }
    }
}
