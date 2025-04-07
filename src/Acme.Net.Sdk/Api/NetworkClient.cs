using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Rpc;

namespace Acme.Net.Sdk.Api
{
    /// <summary>
    /// Client for interacting with network-level operations in the Acme network.
    /// </summary>
    public class NetworkClient : ApiClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkClient"/> class.
        /// </summary>
        /// <param name="rpcClient">The RPC client to use.</param>
        public NetworkClient(AsyncRPCClient rpcClient) : base(rpcClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkClient"/> class using the default RPC client.
        /// </summary>
        public NetworkClient() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkClient"/> class with the specified API endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint URI.</param>
        public NetworkClient(Uri endpoint) : base(endpoint)
        {
        }

        /// <summary>
        /// Gets information about the network.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the network information.</returns>
        public async Task<NetworkStatusResponse> GetNetworkStatusAsync()
        {
            var parameters = new QueryParams
            {
                Url = "acc://network"
            };

            return await QueryAsync<NetworkStatusResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets information about a specific partition in the network.
        /// </summary>
        /// <param name="partitionId">The ID of the partition.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the partition information.</returns>
        public async Task<PartitionResponse> GetPartitionAsync(string partitionId)
        {
            var parameters = new QueryParams
            {
                Url = $"acc://network/{partitionId}"
            };

            return await QueryAsync<PartitionResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a list of all partitions in the network.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of partitions.</returns>
        public async Task<PartitionsResponse> GetPartitionsAsync()
        {
            var parameters = new QueryParams
            {
                Url = "acc://network/partitions"
            };

            return await QueryAsync<PartitionsResponse, QueryParams>(parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the version information of the node.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the version information.</returns>
        public async Task<VersionResponse> GetVersionAsync()
        {
            var parameters = new QueryParams
            {
                Url = "acc://version"
            };

            return await QueryAsync<VersionResponse, QueryParams>(parameters).ConfigureAwait(false);
        }
    }
} 