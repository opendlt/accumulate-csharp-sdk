using System;
using System.Numerics;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for burning tokens transactions.
    /// </summary>
    public class BurnTokensBuilder : TransactionBuilder
    {
        private BigInteger _amount = BigInteger.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="BurnTokensBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public BurnTokensBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the amount of tokens to burn.
        /// </summary>
        /// <param name="amount">The amount as a BigInteger.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public BurnTokensBuilder WithAmount(BigInteger amount)
        {
            if (amount < BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
                
            _amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the amount of tokens to burn.
        /// </summary>
        /// <param name="amount">The amount as a long.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public BurnTokensBuilder WithAmount(long amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
                
            _amount = new BigInteger(amount);
            return this;
        }

        /// <summary>
        /// Sets the amount of tokens to burn.
        /// </summary>
        /// <param name="amount">The amount as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if amount is null or empty.</exception>
        /// <exception cref="FormatException">Thrown if amount is not a valid number.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public BurnTokensBuilder WithAmount(string amount)
        {
            if (string.IsNullOrEmpty(amount))
                throw new ArgumentNullException(nameof(amount));

            var value = BigInteger.Parse(amount);
            return WithAmount(value);
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_amount == BigInteger.Zero)
                throw new InvalidOperationException("Amount must be greater than zero");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var burnTokens = new BurnTokens();
            burnTokens.WithAmount(_amount);

            return burnTokens;
        }
    }
} 