using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for creating a data account.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class CreateDataAccount : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "createDataAccount";

        /// <summary>
        /// Gets or sets the URL of the data account to create.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Gets or sets the list of authorities.
        /// </summary>
        [JsonProperty("authorities")]
        public List<Url> Authorities { get; set; } = new List<Url>();

        /// <summary>
        /// Creates a new instance of CreateDataAccount.
        /// </summary>
        public CreateDataAccount()
        {
        }

        /// <summary>
        /// Sets the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The current instance for method chaining.</returns>
        public CreateDataAccount WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Adds an authority.
        /// </summary>
        /// <param name="authority">The authority URL.</param>
        /// <returns>The current instance for method chaining.</returns>
        public CreateDataAccount AddAuthority(Url authority)
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
            marshaller.WriteUInt(1, TransactionTypeCode.CreateDataAccount);
            
            // Marshal Url as field 2 if present
            if (Url != null)
            {
                marshaller.WriteValue(2, Url);
            }
            
            // Marshal Authorities as field 3 if present
            if (Authorities != null && Authorities.Count > 0)
            {
                foreach (var authority in Authorities)
                {
                    marshaller.WriteValue(3, authority);
                }
            }
            
            return marshaller.GetBytes();
        }
    }
}