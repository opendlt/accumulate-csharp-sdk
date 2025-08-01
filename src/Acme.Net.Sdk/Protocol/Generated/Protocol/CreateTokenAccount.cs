using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for creating a token account.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class CreateTokenAccount : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "createTokenAccount";

        /// <summary>
        /// Gets or sets the URL of the token account to create.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Gets or sets the token URL.
        /// </summary>
        [JsonProperty("tokenUrl")]
        public Url? TokenUrl { get; set; }

        /// <summary>
        /// Gets or sets the key book URL.
        /// </summary>
        [JsonProperty("keyBookUrl")]
        public Url? KeyBookUrl { get; set; }

        /// <summary>
        /// Sets the URL of the token account to create.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateTokenAccount WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL of the token account to create.
        /// </summary>
        /// <param name="url">The URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateTokenAccount WithUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithUrl(new Url(url));
        }

        /// <summary>
        /// Sets the token URL.
        /// </summary>
        /// <param name="tokenUrl">The token URL.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tokenUrl is null.</exception>
        public CreateTokenAccount WithTokenUrl(Url tokenUrl)
        {
            TokenUrl = tokenUrl ?? throw new ArgumentNullException(nameof(tokenUrl));
            return this;
        }

        /// <summary>
        /// Sets the token URL.
        /// </summary>
        /// <param name="tokenUrl">The token URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tokenUrl is null or empty.</exception>
        public CreateTokenAccount WithTokenUrl(string tokenUrl)
        {
            if (string.IsNullOrEmpty(tokenUrl)) throw new ArgumentNullException(nameof(tokenUrl));
            return WithTokenUrl(new Url(tokenUrl));
        }

        /// <summary>
        /// Sets the key book URL.
        /// </summary>
        /// <param name="keyBookUrl">The key book URL.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keyBookUrl is null.</exception>
        public CreateTokenAccount WithKeyBookUrl(Url keyBookUrl)
        {
            KeyBookUrl = keyBookUrl ?? throw new ArgumentNullException(nameof(keyBookUrl));
            return this;
        }

        /// <summary>
        /// Sets the key book URL.
        /// </summary>
        /// <param name="keyBookUrl">The key book URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keyBookUrl is null or empty.</exception>
        public CreateTokenAccount WithKeyBookUrl(string keyBookUrl)
        {
            if (string.IsNullOrEmpty(keyBookUrl)) throw new ArgumentNullException(nameof(keyBookUrl));
            return WithKeyBookUrl(new Url(keyBookUrl));
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1 - value 2 for CreateTokenAccount
            marshaller.WriteUInt(1, TransactionTypeCode.CreateTokenAccount);
            
            // Marshal Url as field 2 if present
            if (Url != null)
            {
                marshaller.WriteValue(2, Url);
            }
            
            // Marshal TokenUrl as field 3 if present
            if (TokenUrl != null)
            {
                marshaller.WriteValue(3, TokenUrl);
            }
            
            // Marshal KeyBookUrl as field 7 if present
            if (KeyBookUrl != null)
            {
                marshaller.WriteValue(7, KeyBookUrl);
            }
            
            return marshaller.GetBytes();
        }
    }
} 