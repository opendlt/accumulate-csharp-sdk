using System;
using System.Collections.Generic;
using System.Numerics;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for issuing tokens transactions.
    /// </summary>
    public class IssueTokensBuilder : TransactionBuilder
    {
        private Url? _recipient;
        private BigInteger _amount;
        private readonly List<TokenRecipient> _recipients = new List<TokenRecipient>();
        private bool _useMultipleRecipients;

        /// <summary>
        /// Initializes a new instance of the <see cref="IssueTokensBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public IssueTokensBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the recipient for token issuing.
        /// </summary>
        /// <param name="recipient">The recipient URL.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null.</exception>
        public IssueTokensBuilder WithRecipient(Url recipient)
        {
            _recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            _useMultipleRecipients = false;
            return this;
        }

        /// <summary>
        /// Sets the recipient for token issuing.
        /// </summary>
        /// <param name="recipient">The recipient URL as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null or empty.</exception>
        public IssueTokensBuilder WithRecipient(string recipient)
        {
            if (string.IsNullOrEmpty(recipient)) throw new ArgumentNullException(nameof(recipient));
            return WithRecipient(new Url(recipient));
        }

        /// <summary>
        /// Sets the amount of tokens to issue.
        /// </summary>
        /// <param name="amount">The amount of tokens to issue.</param>
        /// <returns>This builder for method chaining.</returns>
        public IssueTokensBuilder WithAmount(BigInteger amount)
        {
            _amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the amount of tokens to issue.
        /// </summary>
        /// <param name="amount">The amount of tokens to issue.</param>
        /// <returns>This builder for method chaining.</returns>
        public IssueTokensBuilder WithAmount(ulong amount)
        {
            return WithAmount(new BigInteger(amount));
        }

        /// <summary>
        /// Adds a recipient for advanced token issuing.
        /// </summary>
        /// <param name="url">The recipient URL.</param>
        /// <param name="amount">The amount of tokens to issue to this recipient.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public IssueTokensBuilder AddRecipient(Url url, ulong amount)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            _recipients.Add(new TokenRecipient { Url = url, Amount = amount });
            _useMultipleRecipients = true;
            return this;
        }

        /// <summary>
        /// Adds a recipient for advanced token issuing.
        /// </summary>
        /// <param name="url">The recipient URL as a string.</param>
        /// <param name="amount">The amount of tokens to issue to this recipient.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public IssueTokensBuilder AddRecipient(string url, ulong amount)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return AddRecipient(new Url(url), amount);
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_useMultipleRecipients)
            {
                if (_recipients.Count == 0)
                    throw new InvalidOperationException("At least one recipient must be specified for multiple recipients mode");
            }
            else
            {
                if (_recipient == null)
                    throw new InvalidOperationException("Recipient must be specified");

                if (_amount <= BigInteger.Zero)
                    throw new InvalidOperationException("Amount must be greater than zero");
            }
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var issueTokens = new IssueTokens();

            if (_useMultipleRecipients)
            {
                // inside BuildTransactionBody(), in the _useMultipleRecipients branch:

                foreach (var recipient in _recipients)
                {
                    if (recipient.Amount < 0)
                        throw new InvalidOperationException("Amount must be non-negative");

                    if (recipient.Amount > (System.Numerics.BigInteger)ulong.MaxValue)
                        throw new OverflowException(
                            "Amount exceeds ulong. Update Protocol.Generated.Protocol.IssueTokens to use BigInteger.");

                    issueTokens.AddRecipient(recipient.Url, (ulong)recipient.Amount);
                }

            }
            else
            {
                // Set single recipient and amount
                issueTokens.WithRecipient(_recipient!);
                issueTokens.WithAmount(_amount);
            }

            return issueTokens;
        }
    }
} 