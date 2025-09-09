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
        /// Gets or sets the list of key specifications to include in the key page.
        /// </summary>
        [JsonProperty("keys")]
        public List<KeySpecParams> Keys { get; set; } = new List<KeySpecParams>();

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
            Keys.Add(new KeySpecParams(key));
            return this;
        }

        /// <summary>
        /// Adds a key with a delegate to the key page.
        /// </summary>
        /// <param name="key">The key as a byte array.</param>
        /// <param name="delegate">The delegate URL.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        public CreateKeyPage AddKey(byte[] key, Url? @delegate)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Keys.Add(new KeySpecParams(key, @delegate));
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
            Keys = keys.ConvertAll(k => new KeySpecParams(k));
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
            Keys = new List<KeySpecParams>();
            foreach (var key in keys)
            {
                Keys.Add(new KeySpecParams(key));
            }
            return this;
        }

        /// <summary>
        /// Sets the key specifications to include in the key page.
        /// </summary>
        /// <param name="keySpecs">The list of key specifications.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keySpecs is null.</exception>
        public CreateKeyPage WithKeySpecs(List<KeySpecParams> keySpecs)
        {
            if (keySpecs == null) throw new ArgumentNullException(nameof(keySpecs));
            Keys = new List<KeySpecParams>(keySpecs);
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.CreateKeyPage);
            
            // Note: URL is not in the JavaScript SDK structure for CreateKeyPage
            
            // Marshal Keys as field 2 (repeatable KeySpecParams)
            // Each KeySpecParams has:
            // - Field 1: keyHash (bytes)
            // - Field 2: delegate (URL) - optional
            if (Keys.Count > 0)
            {
                foreach (var keySpec in Keys)
                {
                    // Write each KeySpecParams as a sub-message
                    marshaller.WriteValue(2, keySpec);
                }
            }
            
            return marshaller.GetBytes();
        }
    }
} 