using System;
using Newtonsoft.Json.Linq;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for creating token transactions.
    /// </summary>
    public class CreateTokenBuilder : TransactionBuilder
    {
        private Url? _url;
        private string? _symbol;
        private int _precision = 8;
        private ulong? _supplyLimit;
        private string? _properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTokenBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public CreateTokenBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the URL for the new token.
        /// </summary>
        /// <param name="url">The URL for the token.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateTokenBuilder WithUrl(Url url)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL for the new token.
        /// </summary>
        /// <param name="url">The URL for the token as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateTokenBuilder WithUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithUrl(new Url(url));
        }

        /// <summary>
        /// Sets the token symbol.
        /// </summary>
        /// <param name="symbol">The token symbol.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if symbol is null or empty.</exception>
        public CreateTokenBuilder WithSymbol(string symbol)
        {
            _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            return this;
        }

        /// <summary>
        /// Sets the token precision.
        /// </summary>
        /// <param name="precision">The token precision.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if precision is negative.</exception>
        public CreateTokenBuilder WithPrecision(int precision)
        {
            if (precision < 0) throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be non-negative");
            _precision = precision;
            return this;
        }

        /// <summary>
        /// Sets the supply limit for the token.
        /// </summary>
        /// <param name="supplyLimit">The supply limit for the token.</param>
        /// <returns>This builder for method chaining.</returns>
        public CreateTokenBuilder WithSupplyLimit(ulong supplyLimit)
        {
            _supplyLimit = supplyLimit;
            return this;
        }

        /// <summary>
        /// Sets the properties as a JSON string.
        /// </summary>
        /// <param name="properties">The properties as a JSON string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if properties is null or empty.</exception>
        public CreateTokenBuilder WithProperties(string properties)
        {
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_url == null)
                throw new InvalidOperationException("Token URL must be set");

            if (string.IsNullOrEmpty(_symbol))
                throw new InvalidOperationException("Token symbol must be specified");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var createToken = new CreateToken();
            
            // Set URL
            if (_url != null)
            {
                createToken.WithUrl(_url);
            }
            
            // Set symbol
            if (!string.IsNullOrEmpty(_symbol))
            {
                createToken.WithSymbol(_symbol);
            }
            
            // Set precision
            createToken.WithPrecision(_precision);
            
            // Set supply limit if specified
            if (_supplyLimit.HasValue)
            {
                createToken.WithSupplyLimit(_supplyLimit.Value);
            }
            
            // Set properties if specified
            if (!string.IsNullOrEmpty(_properties))
            {
                createToken.WithProperties(_properties);
            }

            return createToken;
        }
    }
} 