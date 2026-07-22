using System;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Transactions;

namespace Acme.Net.Sdk
{
    /// <summary>
    /// Factory class for creating Acme API clients.
    /// </summary>
    /// <remarks>
    /// Legacy builder-style client. The canonical path is
    /// <see cref="Accumulate"/> + <see cref="Transactions.TxBody"/> +
    /// <see cref="Signing.SmartSigner"/> (see the README and examples/v3).
    /// </remarks>
    [Obsolete("Use the canonical Accumulate + TxBody + SmartSigner API instead (see README and examples/v3).")]
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
        /// Gets a client for interacting with network-level operations in the Acme network.
        /// </summary>
        /// <returns>A network client.</returns>
        public NetworkClient Network()
        {
            return new NetworkClient(_rpcClient);
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
        /// Creates a builder for creating token transactions.
        /// </summary>
        /// <returns>A create token transaction builder.</returns>
        public CreateTokenBuilder CreateTokenBuilder()
        {
            return new CreateTokenBuilder(Tokens());
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
        /// Creates a builder for adding credits to an account.
        /// </summary>
        /// <returns>An add credits transaction builder.</returns>
        public AddCreditsBuilder CreateAddCreditsBuilder()
        {
            return new AddCreditsBuilder(Accounts());
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