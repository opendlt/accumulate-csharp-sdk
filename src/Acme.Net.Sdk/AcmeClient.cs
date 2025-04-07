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

        /// <summary>
        /// Creates a builder for creating token account transactions.
        /// </summary>
        /// <returns>A create token account transaction builder.</returns>
        public CreateTokenAccountBuilder CreateTokenAccountBuilder()
        {
            return new CreateTokenAccountBuilder(Accounts());
        }

        /// <summary>
        /// Creates a builder for burning tokens transactions.
        /// </summary>
        /// <returns>A burn tokens transaction builder.</returns>
        public BurnTokensBuilder CreateBurnTokensBuilder()
        {
            return new BurnTokensBuilder(Tokens());
        }

        /// <summary>
        /// Creates a builder for creating key book transactions.
        /// </summary>
        /// <returns>A create key book transaction builder.</returns>
        public CreateKeyBookBuilder CreateKeyBookBuilder()
        {
            return new CreateKeyBookBuilder(Accounts());
        }

        /// <summary>
        /// Creates a builder for creating key page transactions.
        /// </summary>
        /// <returns>A create key page transaction builder.</returns>
        public CreateKeyPageBuilder CreateKeyPageBuilder()
        {
            return new CreateKeyPageBuilder(Accounts());
        }

        /// <summary>
        /// Creates a builder for writing data to the blockchain.
        /// </summary>
        /// <returns>A new <see cref="WriteDataBuilder"/> instance.</returns>
        public WriteDataBuilder WriteDataBuilder()
        {
            return new WriteDataBuilder(Accounts());
        }

        /// <summary>
        /// Creates a builder for writing data to a specific account.
        /// </summary>
        /// <returns>A new <see cref="WriteDataToBuilder"/> instance.</returns>
        public WriteDataToBuilder WriteDataToBuilder()
        {
            return new WriteDataToBuilder(Accounts());
        }

        // Additional API clients can be added here as needed
    }
} 