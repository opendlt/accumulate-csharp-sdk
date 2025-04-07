using System;
using System.Collections.Generic;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for creating key page transactions.
    /// </summary>
    public class CreateKeyPageBuilder : TransactionBuilder
    {
        private Url? _keyPageUrl;
        private readonly List<byte[]> _keys = new List<byte[]>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateKeyPageBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public CreateKeyPageBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the URL of the key page to create.
        /// </summary>
        /// <param name="url">The URL of the key page.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateKeyPageBuilder WithKeyPageUrl(Url url)
        {
            _keyPageUrl = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL of the key page to create.
        /// </summary>
        /// <param name="url">The URL of the key page as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateKeyPageBuilder WithKeyPageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithKeyPageUrl(new Url(url));
        }

        /// <summary>
        /// Adds a key to the key page.
        /// </summary>
        /// <param name="key">The key as a byte array.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        public CreateKeyPageBuilder AddKey(byte[] key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            _keys.Add(key);
            return this;
        }

        /// <summary>
        /// Sets the keys to include in the key page.
        /// </summary>
        /// <param name="keys">The list of keys as byte arrays.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keys is null.</exception>
        /// <exception cref="ArgumentException">Thrown if keys is empty.</exception>
        public CreateKeyPageBuilder WithKeys(List<byte[]> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) throw new ArgumentException("Keys list cannot be empty", nameof(keys));
            _keys.Clear();
            _keys.AddRange(keys);
            return this;
        }

        /// <summary>
        /// Sets the keys to include in the key page.
        /// </summary>
        /// <param name="keys">The array of keys as byte arrays.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keys is null.</exception>
        /// <exception cref="ArgumentException">Thrown if keys is empty.</exception>
        public CreateKeyPageBuilder WithKeys(byte[][] keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Length == 0) throw new ArgumentException("Keys array cannot be empty", nameof(keys));
            _keys.Clear();
            _keys.AddRange(keys);
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_keyPageUrl == null)
                throw new InvalidOperationException("Key page URL must be set");

            if (_keys.Count == 0)
                throw new InvalidOperationException("At least one key must be specified");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var createKeyPage = new CreateKeyPage();
            
            // Set key page URL
            if (_keyPageUrl != null)
            {
                createKeyPage.WithUrl(_keyPageUrl);
            }
            
            // Add keys
            foreach (var key in _keys)
            {
                createKeyPage.AddKey(key);
            }

            return createKeyPage;
        }
    }
} 