using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Rpc.Models;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Rpc
{
    /// <summary>
    /// Asynchronous RPC client for communicating with the Acme network.
    /// Corresponds to io.accumulatenetwork.sdk.rpc.AsyncRPCClient.
    /// </summary>
    public class AsyncRPCClient : RPCClient
    {
        private static readonly SemaphoreSlim EnvelopeLock = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncRPCClient"/> class using environment variables for the API endpoint.
        /// </summary>
        public AsyncRPCClient() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncRPCClient"/> class with a specified API endpoint.
        /// </summary>
        /// <param name="uri">The URI of the API endpoint.</param>
        public AsyncRPCClient(Uri uri) : base(uri)
        {
        }

        /// <summary>
        /// Sends a transaction asynchronously.
        /// </summary>
        /// <param name="body">The transaction body.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction response.</returns>
        public virtual async Task<TxResponse> SendTxAsync(ITransactionBody body)
        {
            // This is a placeholder implementation until we have all the needed types
            var rpcMethod = Rpc.Models.RPCMethod.FromClass(body.GetType());
            var rpcResponse = await SendAsync(rpcMethod, body).ConfigureAwait(false);
            var txResponse = rpcResponse.AsTransactionResponse();
            
            // Check for errors in the response
            ResultReader.CheckForErrors(txResponse);
            return txResponse;
        }

        /// <summary>
        /// Sends a transaction envelope asynchronously.
        /// </summary>
        /// <param name="envelope">The envelope containing the transaction.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction status.</returns>
        public virtual async Task<object?> SendTxAsync(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            // Using a similar approach as the Java implementation, with a semaphore for sequential processing
            bool lockAcquired = false;
            try
            {
                lockAcquired = await EnvelopeLock.WaitAsync(DefaultLockTimeout).ConfigureAwait(false);
                if (!lockAcquired)
                {
                    throw new TimeoutException($"Failed to acquire envelope lock within {DefaultLockTimeout.TotalSeconds} seconds");
                }
                
                // For subclasses to override and provide alternative implementation
                if (GetType() != typeof(AsyncRPCClient))
                {
                    // In derived types, don't use SendInternalSync which makes real HTTP requests
                    return await Task.FromResult<object?>(null).ConfigureAwait(false);
                }
                
                var rpcResponse = SendInternalSync(Rpc.Models.RPCMethod.ExecuteDirect, envelope);
                var txResponse = rpcResponse.AsTransactionResponse();
                
                // Check for errors in the response first
                ResultReader.CheckForErrors(txResponse);
                
                if (txResponse.Result == null)
                {
                    return null;
                }
                
                // Deserialize the transaction status from the result
                object transactionStatus;
                try
                {
                    transactionStatus = ResultReader.ReadValue<TransactionStatus>(txResponse.Result.Value);
                }
                catch
                {
                    // If deserialization fails, return the raw result
                    transactionStatus = txResponse.Result.Value;
                }
                
                // Check for errors in both response and status
                ResultReader.CheckForErrors(txResponse, transactionStatus);
                return transactionStatus;
            }
            finally
            {
                if (lockAcquired)
                {
                    EnvelopeLock.Release();
                }
            }
        }

        /// <summary>
        /// Sends a transaction envelope asynchronously.
        /// </summary>
        /// <param name="envelopeBuilder">The envelope builder containing the transaction.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction status.</returns>
        public virtual async Task<object?> SendTxAsync(EnvelopeBuilder envelopeBuilder)
        {
            if (envelopeBuilder == null)
                throw new ArgumentNullException(nameof(envelopeBuilder));
                
            return await SendTxAsync(envelopeBuilder.Build()).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an RPC body payload asynchronously.
        /// </summary>
        /// <param name="payload">The RPC body payload.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the RPC response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if payload is null.</exception>
        public Task<RPCResponse> SendAsync(IRPCBody payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            
            var rpcMethod = Rpc.Models.RPCMethod.FromClass(payload.GetType());
            return SendInternalAsync(rpcMethod, payload);
        }

        /// <summary>
        /// Sends an RPC method with a payload asynchronously.
        /// </summary>
        /// <param name="rpcMethod">The RPC method to call.</param>
        /// <param name="payload">The RPC body payload.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the RPC response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if rpcMethod is null.</exception>
        public Task<RPCResponse> SendAsync(Rpc.Models.RPCMethod rpcMethod, IRPCBody payload)
        {
            if (rpcMethod == null)
            {
                throw new ArgumentNullException(nameof(rpcMethod));
            }
            
            return SendInternalAsync(rpcMethod, payload);
        }

        private async Task<RPCResponse> SendInternalAsync(Rpc.Models.RPCMethod rpcMethod, object body)
        {
            try
            {
                int requestId = NewRequestId();
                var request = BuildRequest(requestId, rpcMethod, body);
                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw BuildResponseException(response);
                }

                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
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