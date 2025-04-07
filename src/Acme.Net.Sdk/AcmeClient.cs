using System;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Transactions;

namespace Acme.Net.Sdk
{
    /// <summary>
    /// Factory class for creating Acme API clients.
    /// </summary>
    public class AcmeClient
    {
        private readonly AsyncRPCClient _rpcClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeClient"/> class using environment variables for the API endpoint.
        /// </summary>
        public AcmeClient()
        {
            _rpcClient = new AsyncRPCClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeClient"/> class with a specified API endpoint.
        /// </summary>
        /// <param name="endpoint">The URI of the API endpoint.</param>
        public AcmeClient(Uri endpoint)
        {
            _rpcClient = new AsyncRPCClient(endpoint);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeClient"/> class with a specified API endpoint URL.
        /// </summary>
        /// <param name="endpoint">The URL of the API endpoint.</param>
        public AcmeClient(string endpoint) : this(new Uri(endpoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeClient"/> class with a specified RPC client.
        /// </summary>
        /// <param name="rpcClient">The RPC client to use.</param>
        public AcmeClient(AsyncRPCClient rpcClient)
        {
            _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        }

        /// <summary>
        /// Gets a client for interacting with accounts in the Acme network.
        /// </summary>
        /// <returns>An accounts client.</returns>
        public AccountsClient Accounts()
        {
            return new AccountsClient(_rpcClient);
        }

        /// <summary>
        /// Gets a client for interacting with tokens in the Acme network.
        /// </summary>
        /// <returns>A tokens client.</returns>
        public TokensClient Tokens()
        {
            return new TokensClient(_rpcClient);
        }

        /// <summary>
        /// Creates a builder for sending tokens transactions.
        /// </summary>
        /// <returns>A send tokens transaction builder.</returns>
        public SendTokensBuilder CreateSendTokensBuilder()
        {
            return new SendTokensBuilder(Tokens());
        }

        /// <summary>
        /// Creates a builder for issuing tokens transactions.
        /// </summary>
        /// <returns>An issue tokens transaction builder.</returns>
        public IssueTokensBuilder CreateIssueTokensBuilder()
        {
            return new IssueTokensBuilder(Tokens());
        }

        // Additional API clients can be added here as needed
    }
} 