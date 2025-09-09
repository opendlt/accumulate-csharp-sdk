using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for sending tokens.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class SendTokens : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "sendTokens";

        /// <summary>
        /// Gets or sets the list of token recipients.
        /// </summary>
        [JsonProperty("to")]
        public List<TokenRecipient> Recipients { get; set; } = new List<TokenRecipient>();

        /// <summary>
        /// Gets or sets the metadata hash.
        /// </summary>
        [JsonProperty("hash")]
        public string? Hash { get; set; }

        /// <summary>
        /// Gets or sets the metadata as a raw JSON value.
        /// </summary>
        [JsonProperty("meta")]
        public JRaw? Meta { get; set; }

        /// <summary>
        /// Sets the hash value.
        /// </summary>
        /// <param name="value">The hash value as a byte array.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens WithHash(byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Hash = Convert.ToBase64String(value);
            return this;
        }

        /// <summary>
        /// Sets the hash value.
        /// </summary>
        /// <param name="value">The hash value as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens WithHash(string value)
        {
            Hash = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        /// <summary>
        /// Sets the metadata as raw JSON.
        /// </summary>
        /// <param name="value">The metadata as a JRaw instance.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens WithMeta(JRaw value)
        {
            Meta = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        /// <summary>
        /// Sets the metadata as a JSON string.
        /// </summary>
        /// <param name="value">The metadata as a JSON string.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens WithMeta(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            Meta = new JRaw(value);
            return this;
        }

        /// <summary>
        /// Sets the recipients.
        /// </summary>
        /// <param name="recipients">An array of token recipients.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens WithRecipients(params TokenRecipient[] recipients)
        {
            if (recipients == null) throw new ArgumentNullException(nameof(recipients));
            Recipients.Clear();
            Recipients.AddRange(recipients);
            return this;
        }

        /// <summary>
        /// Adds a recipient.
        /// </summary>
        /// <param name="recipient">The token recipient to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens AddRecipient(TokenRecipient recipient)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            Recipients.Add(recipient);
            return this;
        }

        /// <summary>
        /// Adds a recipient.
        /// </summary>
        /// <param name="url">The recipient URL.</param>
        /// <param name="amount">The amount of tokens to send.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens AddRecipient(Url url, ulong amount)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            Recipients.Add(new TokenRecipient { Url = url, Amount = amount });
            return this;
        }

        /// <summary>
        /// Adds a recipient.
        /// </summary>
        /// <param name="url">The recipient URL as a string.</param>
        /// <param name="amount">The amount of tokens to send.</param>
        /// <returns>This instance for method chaining.</returns>
        public SendTokens AddRecipient(string url, ulong amount)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return AddRecipient(new Url(url), amount);
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Field 1: type
            marshaller.WriteUInt(1, TransactionTypeCode.SendTokens);
            
            // Field 2: hash (optional)
            if (!string.IsNullOrEmpty(Hash))
            {
                var hashBytes = Convert.FromBase64String(Hash);
                marshaller.WriteBytes(2, hashBytes);
            }
            
            // Field 3: meta (optional)
            if (Meta != null)
            {
                // JRaw contains the raw JSON string, use its Value property
                var metaString = Meta.Value?.ToString() ?? Meta.ToString();
                var metaBytes = System.Text.Encoding.UTF8.GetBytes(metaString);
                marshaller.WriteBytes(3, metaBytes);
            }
            
            // Field 4: recipients (repeatable)
            if (Recipients != null && Recipients.Count > 0)
            {
                foreach (var recipient in Recipients)
                {
                    marshaller.WriteValue(4, recipient);
                }
            }
            
            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Represents a token recipient for a SendTokens transaction.
    /// </summary>
    public class TokenRecipient : IMarshallable
    {
        /// <summary>
        /// Gets or sets the recipient URL.
        /// </summary>
        [JsonProperty("url")]
        public Url Url { get; set; }

        /// <summary>
        /// Gets or sets the amount of tokens to send.
        /// </summary>
        [JsonProperty("amount")]
        public ulong Amount { get; set; }
        
        public TokenRecipient()
        {
            // Initialize with a valid URL that has an authority (host)
            Url = new Url("acc://example.acme");
        }
        
        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Field 1: url
            if (Url != null)
            {
                marshaller.WriteValue(1, Url);
            }
            
            // Field 2: amount (as BigInt bytes)
            // Convert amount to bytes in big-endian format
            var bigInt = new System.Numerics.BigInteger(Amount);
            var amountBytes = bigInt.ToByteArray();
            
            // BigInteger.ToByteArray() returns little-endian with potential sign byte
            // We need to:
            // 1. Remove the sign byte if present (for positive numbers)
            // 2. Reverse to big-endian
            
            // Check if we have a sign byte (last byte is 0 for positive numbers > 127)
            bool hasSignByte = amountBytes.Length > 1 && amountBytes[amountBytes.Length - 1] == 0;
            int length = hasSignByte ? amountBytes.Length - 1 : amountBytes.Length;
            
            var result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = amountBytes[length - 1 - i];
            }
            
            marshaller.WriteBytes(2, result);
            
            return marshaller.GetBytes();
        }
    }
} 