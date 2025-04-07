using System;
using System.Collections.Generic;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for sending tokens transactions.
    /// </summary>
    public class SendTokensBuilder : TransactionBuilder
    {
        private readonly List<TokenRecipient> _recipients = new List<TokenRecipient>();
        private string? _hash;
        private string? _meta;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTokensBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public SendTokensBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Adds a recipient for token sending.
        /// </summary>
        /// <param name="url">The recipient URL.</param>
        /// <param name="amount">The amount of tokens to send.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public SendTokensBuilder AddRecipient(Url url, ulong amount)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            _recipients.Add(new TokenRecipient { Url = url, Amount = amount });
            return this;
        }

        /// <summary>
        /// Adds a recipient for token sending.
        /// </summary>
        /// <param name="url">The recipient URL as a string.</param>
        /// <param name="amount">The amount of tokens to send.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public SendTokensBuilder AddRecipient(string url, ulong amount)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return AddRecipient(new Url(url), amount);
        }

        /// <summary>
        /// Sets the metadata hash.
        /// </summary>
        /// <param name="hash">The hash value.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if hash is null or empty.</exception>
        public SendTokensBuilder WithHash(string hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
            return this;
        }

        /// <summary>
        /// Sets the metadata.
        /// </summary>
        /// <param name="meta">The metadata as a JSON string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if meta is null or empty.</exception>
        public SendTokensBuilder WithMeta(string meta)
        {
            _meta = meta ?? throw new ArgumentNullException(nameof(meta));
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_recipients.Count == 0)
                throw new InvalidOperationException("At least one recipient must be specified");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var sendTokens = new SendTokens();
            
            // Add all recipients
            foreach (var recipient in _recipients)
            {
                sendTokens.AddRecipient(recipient);
            }

            // Set hash if provided
            if (!string.IsNullOrEmpty(_hash))
            {
                sendTokens.WithHash(_hash);
            }

            // Set meta if provided
            if (!string.IsNullOrEmpty(_meta))
            {
                sendTokens.WithMeta(_meta);
            }

            return sendTokens;
        }
    }
} 