using System;
using System.Collections.Generic;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for creating key book transactions.
    /// </summary>
    public class CreateKeyBookBuilder : TransactionBuilder
    {
        private Url? _keyBookUrl;
        private readonly List<Url> _pageUrls = new List<Url>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateKeyBookBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public CreateKeyBookBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the URL of the key book to create.
        /// </summary>
        /// <param name="url">The URL of the key book.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateKeyBookBuilder WithKeyBookUrl(Url url)
        {
            _keyBookUrl = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL of the key book to create.
        /// </summary>
        /// <param name="url">The URL of the key book as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateKeyBookBuilder WithKeyBookUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithKeyBookUrl(new Url(url));
        }

        /// <summary>
        /// Adds a key page URL to the key book.
        /// </summary>
        /// <param name="pageUrl">The URL of the key page to add.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrl is null.</exception>
        public CreateKeyBookBuilder AddKeyPage(Url pageUrl)
        {
            if (pageUrl == null) throw new ArgumentNullException(nameof(pageUrl));
            _pageUrls.Add(pageUrl);
            return this;
        }

        /// <summary>
        /// Adds a key page URL to the key book.
        /// </summary>
        /// <param name="pageUrl">The URL of the key page to add as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrl is null or empty.</exception>
        public CreateKeyBookBuilder AddKeyPage(string pageUrl)
        {
            if (string.IsNullOrEmpty(pageUrl)) throw new ArgumentNullException(nameof(pageUrl));
            return AddKeyPage(new Url(pageUrl));
        }

        /// <summary>
        /// Sets the key page URLs to include in the key book.
        /// </summary>
        /// <param name="pageUrls">The list of key page URLs.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrls is null.</exception>
        public CreateKeyBookBuilder WithKeyPages(List<Url> pageUrls)
        {
            if (pageUrls == null) throw new ArgumentNullException(nameof(pageUrls));
            _pageUrls.Clear();
            _pageUrls.AddRange(pageUrls);
            return this;
        }

        /// <summary>
        /// Sets the key page URLs to include in the key book.
        /// </summary>
        /// <param name="pageUrls">The array of key page URLs.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrls is null.</exception>
        public CreateKeyBookBuilder WithKeyPages(Url[] pageUrls)
        {
            if (pageUrls == null) throw new ArgumentNullException(nameof(pageUrls));
            _pageUrls.Clear();
            _pageUrls.AddRange(pageUrls);
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_keyBookUrl == null)
                throw new InvalidOperationException("Key book URL must be set");

            if (_pageUrls.Count == 0)
                throw new InvalidOperationException("At least one key page URL must be specified");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var createKeyBook = new CreateKeyBook();
            
            // Set key book URL
            if (_keyBookUrl != null)
            {
                createKeyBook.WithUrl(_keyBookUrl);
            }
            
            // Add key page URLs
            foreach (var pageUrl in _pageUrls)
            {
                createKeyBook.AddPage(pageUrl);
            }

            return createKeyBook;
        }
    }
} 