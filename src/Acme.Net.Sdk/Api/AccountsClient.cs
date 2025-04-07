using System;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Rpc;

namespace Acme.Net.Sdk.Api
{
    /// <summary>
    /// Client for interacting with accounts in the Acme network.
    /// </summary>
    public class AccountsClient : ApiClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountsClient"/> class.
        /// </summary>
        /// <param name="rpcClient">The RPC client to use.</param>
        public AccountsClient(AsyncRPCClient rpcClient) : base(rpcClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountsClient"/> class using the default RPC client.
        /// </summary>
        public AccountsClient() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountsClient"/> class with the specified API endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint URI.</param>
        public AccountsClient(Uri endpoint) : base(endpoint)
        {
        }

        /// <summary>
        /// Gets information about an account.
        /// </summary>
        /// <param name="url">The account URL.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the account information.</returns>
        public async Task<AccountResponse> GetAccountAsync(string url)
        {
            var parameters = new QueryParams
            {
                Url = url
            };

            return await QueryAsync<AccountResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the transactions for an account.
        /// </summary>
        /// <param name="url">The account URL.</param>
        /// <param name="count">The maximum number of transactions to retrieve.</param>
        /// <param name="start">The start index for pagination.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the account's transactions.</returns>
        public async Task<TransactionResponse> GetTransactionsAsync(string url, int count = 10, int start = 0)
        {
            var parameters = new QueryParams
            {
                Url = $"{url}/transactions",
                Count = count,
                Start = start
            };

            return await QueryAsync<TransactionResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new account.
        /// </summary>
        /// <param name="createAccountParams">The parameters for creating the account.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public async Task<TxResponse> CreateAccountAsync(ITransactionBody createAccountParams)
        {
            // This would use the appropriate transaction type in a real implementation
            return await ExecuteAsync(createAccountParams).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Parameters for querying the Acme API.
    /// </summary>
    public class QueryParams : IRPCBody
    {
        /// <summary>
        /// Gets or sets the URL to query.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of items to retrieve.
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets the start index for pagination.
        /// </summary>
        public int? Start { get; set; }
    }
} 