using System;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Rpc.Models;

namespace Acme.Net.Sdk.Api
{
    /// <summary>
    /// Base class for all API clients.
    /// </summary>
    public abstract class ApiClient
    {
        /// <summary>
        /// Gets the RPC client used to communicate with the API.
        /// </summary>
        protected AsyncRPCClient RpcClient { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient"/> class.
        /// </summary>
        /// <param name="rpcClient">The RPC client to use.</param>
        protected ApiClient(AsyncRPCClient rpcClient)
        {
            RpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient"/> class using the default RPC client.
        /// </summary>
        protected ApiClient() : this(new AsyncRPCClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient"/> class with the specified API endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint URI.</param>
        protected ApiClient(Uri endpoint) : this(new AsyncRPCClient(endpoint))
        {
        }

        /// <summary>
        /// Executes a transaction and returns the transaction response.
        /// </summary>
        /// <param name="body">The transaction body.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public virtual async Task<TxResponse> ExecuteAsync(ITransactionBody body)
        {
            return await RpcClient.SendTxAsync(body).ConfigureAwait(false);
        }

        /// <summary>
        /// Queries the API and returns the response as the specified type.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <typeparam name="TParams">The type of the query parameters.</typeparam>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the query response.</returns>
        protected async Task<TResponse> QueryAsync<TResponse, TParams>(TParams parameters)
            where TParams : IRPCBody
        {
            var response = await RpcClient.SendAsync(RPCMethod.Query, parameters).ConfigureAwait(false);
            
            // We would normally use a JsonConverter or other deserialization logic here
            // For now, we'll use a simple approach with System.Text.Json
            if (response.Result.HasValue)
            {
                var resultJson = response.Result.Value.GetRawText();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return System.Text.Json.JsonSerializer.Deserialize<TResponse>(resultJson, options) 
                    ?? throw new InvalidOperationException("Failed to deserialize response");
            }
            
            throw new InvalidOperationException("Query response is empty");
        }

        /// <summary>
        /// Queries the chain API and returns the response as the specified type.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <typeparam name="TParams">The type of the query parameters.</typeparam>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the query response.</returns>
        protected async Task<TResponse> QueryChainAsync<TResponse, TParams>(TParams parameters)
            where TParams : IRPCBody
        {
            var response = await RpcClient.SendAsync(RPCMethod.QueryChain, parameters).ConfigureAwait(false);
            
            if (response.Result.HasValue)
            {
                var resultJson = response.Result.Value.GetRawText();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return System.Text.Json.JsonSerializer.Deserialize<TResponse>(resultJson, options) 
                    ?? throw new InvalidOperationException("Failed to deserialize response");
            }
            
            throw new InvalidOperationException("Query response is empty");
        }
    }
} 