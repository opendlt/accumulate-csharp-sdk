using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for the ACME faucet.
    /// Produces a synthetic deposit tokens transaction that deposits ACME tokens into a lite token account.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class AcmeFaucet : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "acmeFaucet";

        /// <summary>
        /// Gets or sets the URL of the lite token account to receive tokens.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Creates a new instance of AcmeFaucet.
        /// </summary>
        public AcmeFaucet()
        {
        }

        /// <summary>
        /// Sets the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The current instance for method chaining.</returns>
        public AcmeFaucet WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL.
        /// </summary>
        /// <param name="url">The URL as a string.</param>
        /// <returns>The current instance for method chaining.</returns>
        public AcmeFaucet WithUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithUrl(new Url(url));
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.AcmeFaucet);
            
            // Marshal Url as field 2 if present
            if (Url != null)
            {
                marshaller.WriteValue(2, Url);
            }
            
            return marshaller.GetBytes();
        }
    }
}