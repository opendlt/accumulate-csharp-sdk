using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for creating a key book.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class CreateKeyBook : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "createKeyBook";

        /// <summary>
        /// Gets or sets the URL of the key book to create.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Gets or sets the public key hash (optional).
        /// </summary>
        [JsonProperty("publicKeyHash")]
        public byte[]? PublicKeyHash { get; set; }

        /// <summary>
        /// Gets or sets the list of key page URLs to include in the key book.
        /// </summary>
        [JsonProperty("pages")]
        public List<Url> Pages { get; set; } = new List<Url>();

        /// <summary>
        /// Sets the URL of the key book to create.
        /// </summary>
        /// <param name="url">The URL of the key book.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateKeyBook WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL of the key book to create.
        /// </summary>
        /// <param name="url">The URL of the key book as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateKeyBook WithUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithUrl(new Url(url));
        }

        /// <summary>
        /// Adds a key page URL to the key book.
        /// </summary>
        /// <param name="pageUrl">The URL of the key page to add.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrl is null.</exception>
        public CreateKeyBook AddPage(Url pageUrl)
        {
            if (pageUrl == null) throw new ArgumentNullException(nameof(pageUrl));
            Pages.Add(pageUrl);
            return this;
        }

        /// <summary>
        /// Adds a key page URL to the key book.
        /// </summary>
        /// <param name="pageUrl">The URL of the key page to add as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrl is null or empty.</exception>
        public CreateKeyBook AddPage(string pageUrl)
        {
            if (string.IsNullOrEmpty(pageUrl)) throw new ArgumentNullException(nameof(pageUrl));
            return AddPage(new Url(pageUrl));
        }

        /// <summary>
        /// Sets the key page URLs to include in the key book.
        /// </summary>
        /// <param name="pageUrls">The list of key page URLs.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrls is null.</exception>
        public CreateKeyBook WithPages(List<Url> pageUrls)
        {
            if (pageUrls == null) throw new ArgumentNullException(nameof(pageUrls));
            Pages = new List<Url>(pageUrls);
            return this;
        }

        /// <summary>
        /// Sets the key page URLs to include in the key book.
        /// </summary>
        /// <param name="pageUrls">The array of key page URLs.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pageUrls is null.</exception>
        public CreateKeyBook WithPages(Url[] pageUrls)
        {
            if (pageUrls == null) throw new ArgumentNullException(nameof(pageUrls));
            Pages = new List<Url>(pageUrls);
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.CreateKeyBook);
            
            // Marshal Url as field 2 if present
            if (Url != null)
            {
                marshaller.WriteValue(2, Url);
            }
            
            // Field 3: publicKeyHash (bytes) - optional
            if (PublicKeyHash != null && PublicKeyHash.Length > 0)
            {
                marshaller.WriteBytes(3, PublicKeyHash);
            }
            
            // Field 5: authorities (repeatable URL array)
            // In JavaScript SDK, Pages are passed as authorities
            if (Pages.Count > 0)
            {
                foreach (var page in Pages)
                {
                    marshaller.WriteUrl(5, page);
                }
            }
            
            return marshaller.GetBytes();
        }
    }
} 