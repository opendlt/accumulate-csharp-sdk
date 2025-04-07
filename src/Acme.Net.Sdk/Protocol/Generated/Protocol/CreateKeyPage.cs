using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for creating a key page.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class CreateKeyPage : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "createKeyPage";

        /// <summary>
        /// Gets or sets the URL of the key page to create.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Gets or sets the list of keys to include in the key page.
        /// </summary>
        [JsonProperty("keys")]
        public List<byte[]> Keys { get; set; } = new List<byte[]>();

        /// <summary>
        /// Sets the URL of the key page to create.
        /// </summary>
        /// <param name="url">The URL of the key page.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public CreateKeyPage WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL of the key page to create.
        /// </summary>
        /// <param name="url">The URL of the key page as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        public CreateKeyPage WithUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithUrl(new Url(url));
        }

        /// <summary>
        /// Adds a key to the key page.
        /// </summary>
        /// <param name="key">The key as a byte array.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        public CreateKeyPage AddKey(byte[] key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Keys.Add(key);
            return this;
        }

        /// <summary>
        /// Sets the keys to include in the key page.
        /// </summary>
        /// <param name="keys">The list of keys as byte arrays.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keys is null.</exception>
        public CreateKeyPage WithKeys(List<byte[]> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            Keys = new List<byte[]>(keys);
            return this;
        }

        /// <summary>
        /// Sets the keys to include in the key page.
        /// </summary>
        /// <param name="keys">The array of keys as byte arrays.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keys is null.</exception>
        public CreateKeyPage WithKeys(byte[][] keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            Keys = new List<byte[]>(keys);
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal Url if present
            if (Url != null)
            {
                marshaller.WriteValue(1, Url);
            }
            
            // Marshal Keys
            if (Keys.Count > 0)
            {
                foreach (var key in Keys)
                {
                    marshaller.WriteValue(2, key);
                }
            }
            
            return marshaller.ToArray();
        }
    }
} 