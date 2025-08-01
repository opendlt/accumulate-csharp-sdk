using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for creating an identity (ADI).
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class CreateIdentity : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "createIdentity";

        /// <summary>
        /// Gets or sets the URL of the identity to create.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Gets or sets the key hash.
        /// </summary>
        [JsonProperty("keyHash")]
        public byte[]? KeyHash { get; set; }

        /// <summary>
        /// Gets or sets the key book URL.
        /// </summary>
        [JsonProperty("keyBookUrl")]
        public Url? KeyBookUrl { get; set; }

        /// <summary>
        /// Gets or sets the list of authorities.
        /// </summary>
        [JsonProperty("authorities")]
        public List<Url> Authorities { get; set; } = new List<Url>();

        /// <summary>
        /// Creates a new instance of CreateIdentity.
        /// </summary>
        public CreateIdentity()
        {
        }

        /// <summary>
        /// Sets the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The current instance for method chaining.</returns>
        public CreateIdentity WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the key hash.
        /// </summary>
        /// <param name="keyHash">The key hash.</param>
        /// <returns>The current instance for method chaining.</returns>
        public CreateIdentity WithKeyHash(byte[] keyHash)
        {
            KeyHash = keyHash ?? throw new ArgumentNullException(nameof(keyHash));
            return this;
        }

        /// <summary>
        /// Sets the key book URL.
        /// </summary>
        /// <param name="keyBookUrl">The key book URL.</param>
        /// <returns>The current instance for method chaining.</returns>
        public CreateIdentity WithKeyBookUrl(Url keyBookUrl)
        {
            KeyBookUrl = keyBookUrl ?? throw new ArgumentNullException(nameof(keyBookUrl));
            return this;
        }

        /// <summary>
        /// Adds an authority.
        /// </summary>
        /// <param name="authority">The authority URL.</param>
        /// <returns>The current instance for method chaining.</returns>
        public CreateIdentity AddAuthority(Url authority)
        {
            if (authority == null) throw new ArgumentNullException(nameof(authority));
            Authorities.Add(authority);
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.CreateIdentity);
            
            // Marshal Url as field 2 if present
            if (Url != null)
            {
                marshaller.WriteValue(2, Url);
            }
            
            // Marshal KeyHash as field 3 if present
            if (KeyHash != null)
            {
                marshaller.WriteValue(3, KeyHash);
            }
            
            // Marshal KeyBookUrl as field 4 if present
            if (KeyBookUrl != null)
            {
                marshaller.WriteValue(4, KeyBookUrl);
            }
            
            // Marshal Authorities as field 6 if present
            if (Authorities != null && Authorities.Count > 0)
            {
                foreach (var authority in Authorities)
                {
                    marshaller.WriteValue(6, authority);
                }
            }
            
            return marshaller.GetBytes();
        }
    }
}