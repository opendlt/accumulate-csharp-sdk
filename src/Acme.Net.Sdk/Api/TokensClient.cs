using System;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Rpc;

namespace Acme.Net.Sdk.Api
{
    /// <summary>
    /// Client for interacting with tokens in the Acme network.
    /// </summary>
    public class TokensClient : ApiClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokensClient"/> class.
        /// </summary>
        /// <param name="rpcClient">The RPC client to use.</param>
        public TokensClient(AsyncRPCClient rpcClient) : base(rpcClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokensClient"/> class using the default RPC client.
        /// </summary>
        public TokensClient() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokensClient"/> class with the specified API endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint URI.</param>
        public TokensClient(Uri endpoint) : base(endpoint)
        {
        }

        /// <summary>
        /// Gets information about a token.
        /// </summary>
        /// <param name="url">The token URL.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the token information.</returns>
        public async Task<TokenResponse> GetTokenAsync(string url)
        {
            var parameters = new QueryParams
            {
                Url = url
            };

            return await QueryAsync<TokenResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets information about all tokens for an issuer.
        /// </summary>
        /// <param name="issuerUrl">The issuer URL.</param>
        /// <param name="count">The maximum number of tokens to retrieve.</param>
        /// <param name="start">The start index for pagination.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tokens information.</returns>
        public async Task<TokensResponse> GetTokensAsync(string issuerUrl, int count = 10, int start = 0)
        {
            var parameters = new QueryParams
            {
                Url = $"{issuerUrl}/tokens",
                Count = count,
                Start = start
            };

            return await QueryAsync<TokensResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new token.
        /// </summary>
        /// <param name="createTokenParams">The parameters for creating the token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public async Task<TxResponse> CreateTokenAsync(ITransactionBody createTokenParams)
        {
            // This would use the appropriate transaction type in a real implementation
            return await ExecuteAsync(createTokenParams).ConfigureAwait(false);
        }

        /// <summary>
        /// Issues new tokens.
        /// </summary>
        /// <param name="issueTokenParams">The parameters for issuing tokens.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public async Task<TxResponse> IssueTokensAsync(ITransactionBody issueTokenParams)
        {
            // This would use the appropriate transaction type in a real implementation
            return await ExecuteAsync(issueTokenParams).ConfigureAwait(false);
        }

        /// <summary>
        /// Burns tokens.
        /// </summary>
        /// <param name="burnTokenParams">The parameters for burning tokens.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public async Task<TxResponse> BurnTokensAsync(ITransactionBody burnTokenParams)
        {
            // This would use the appropriate transaction type in a real implementation
            return await ExecuteAsync(burnTokenParams).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends tokens to another account.
        /// </summary>
        /// <param name="sendTokenParams">The parameters for sending tokens.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public async Task<TxResponse> SendTokensAsync(ITransactionBody sendTokenParams)
        {
            // This would use the appropriate transaction type in a real implementation
            return await ExecuteAsync(sendTokenParams).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Represents a response containing information about a token.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the token URL.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token symbol.
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token precision.
        /// </summary>
        public int Precision { get; set; }

        /// <summary>
        /// Gets or sets the token's issuer URL.
        /// </summary>
        public string IssuerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token's supply.
        /// </summary>
        public long Supply { get; set; }
    }

    /// <summary>
    /// Represents a response containing information about multiple tokens.
    /// </summary>
    public class TokensResponse
    {
        /// <summary>
        /// Gets or sets the list of tokens.
        /// </summary>
        public TokenResponse[] Items { get; set; } = Array.Empty<TokenResponse>();

        /// <summary>
        /// Gets or sets the total number of tokens available.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the start index of this response.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens in this response.
        /// </summary>
        public int Count { get; set; }
    }
} 