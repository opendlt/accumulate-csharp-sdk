using System;
using System.Numerics;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for adding credits transactions.
    /// </summary>
    public class AddCreditsBuilder : TransactionBuilder
    {
        private Url? _recipient;
        private BigInteger _amount = BigInteger.Zero;
        private string? _oracle;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddCreditsBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public AddCreditsBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the recipient account URL.
        /// </summary>
        /// <param name="recipient">The recipient account URL.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null.</exception>
        public AddCreditsBuilder WithRecipient(Url recipient)
        {
            _recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            return this;
        }

        /// <summary>
        /// Sets the recipient account URL.
        /// </summary>
        /// <param name="recipient">The recipient account URL as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null or empty.</exception>
        public AddCreditsBuilder WithRecipient(string recipient)
        {
            if (string.IsNullOrEmpty(recipient)) throw new ArgumentNullException(nameof(recipient));
            return WithRecipient(new Url(recipient));
        }

        /// <summary>
        /// Sets the amount of credits to add.
        /// </summary>
        /// <param name="amount">The amount of credits as a BigInteger.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public AddCreditsBuilder WithAmount(BigInteger amount)
        {
            if (amount < BigInteger.Zero) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
            _amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the amount of credits to add.
        /// </summary>
        /// <param name="amount">The amount of credits as a long.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public AddCreditsBuilder WithAmount(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
            return WithAmount(new BigInteger(amount));
        }

        /// <summary>
        /// Sets the oracle signature.
        /// </summary>
        /// <param name="oracle">The oracle signature.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if oracle is null or empty.</exception>
        public AddCreditsBuilder WithOracle(string oracle)
        {
            _oracle = oracle ?? throw new ArgumentNullException(nameof(oracle));
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_recipient == null)
                throw new InvalidOperationException("Recipient account URL must be set");

            if (_amount <= BigInteger.Zero)
                throw new InvalidOperationException("Amount must be greater than zero");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var addCredits = new AddCredits();
            
            // Set recipient
            if (_recipient != null)
            {
                addCredits.WithRecipient(_recipient);
            }
            
            // Set amount
            addCredits.WithAmount(_amount);
            
            // Set oracle if specified
            if (!string.IsNullOrEmpty(_oracle))
            {
                addCredits.WithOracle(_oracle);
            }

            return addCredits;
        }
    }
} 