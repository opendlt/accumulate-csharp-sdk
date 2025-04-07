using System;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for creating token account transactions.
    /// </summary>
    public class CreateTokenAccountBuilder : TransactionBuilder
    {
        private Url? _accountUrl;
        private Url? _tokenUrl;
        private Url? _keyBookUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTokenAccountBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public CreateTokenAccountBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the URL of the token account to create.
        /// </summary>
        /// <param name="url">The URL of the token account.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateTokenAccountBuilder WithAccountUrl(Url url)
        {
            _accountUrl = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL of the token account to create.
        /// </summary>
        /// <param name="url">The URL of the token account as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateTokenAccountBuilder WithAccountUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithAccountUrl(new Url(url));
        }

        /// <summary>
        /// Sets the token URL.
        /// </summary>
        /// <param name="url">The token URL.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateTokenAccountBuilder WithTokenUrl(Url url)
        {
            _tokenUrl = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the token URL.
        /// </summary>
        /// <param name="url">The token URL as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateTokenAccountBuilder WithTokenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithTokenUrl(new Url(url));
        }

        /// <summary>
        /// Sets the key book URL.
        /// </summary>
        /// <param name="url">The key book URL.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateTokenAccountBuilder WithKeyBookUrl(Url url)
        {
            _keyBookUrl = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the key book URL.
        /// </summary>
        /// <param name="url">The key book URL as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateTokenAccountBuilder WithKeyBookUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithKeyBookUrl(new Url(url));
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_accountUrl == null)
                throw new InvalidOperationException("Account URL must be set");

            if (_tokenUrl == null)
                throw new InvalidOperationException("Token URL must be set");

            // KeyBookUrl is optional if the token account has the same key book as the token
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var createAccount = new CreateTokenAccount();
            
            // Set required properties
            createAccount.WithUrl(_accountUrl);
            createAccount.WithTokenUrl(_tokenUrl);
            
            // Set optional properties if provided
            if (_keyBookUrl != null)
            {
                createAccount.WithKeyBookUrl(_keyBookUrl);
            }

            return createAccount;
        }
    }
} 