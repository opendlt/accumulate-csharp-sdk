using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for issuing tokens.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class IssueTokens : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "issueTokens";

        /// <summary>
        /// Gets or sets the recipient URL.
        /// </summary>
        [JsonProperty("recipient")]
        public Url? Recipient { get; set; }

        /// <summary>
        /// Gets or sets the amount of tokens to issue.
        /// </summary>
        [JsonProperty("amount")]
        public BigInteger Amount { get; set; }

        /// <summary>
        /// Gets or sets the list of token recipients for advanced issuing.
        /// </summary>
        [JsonProperty("to")]
        public List<TokenRecipient>? Recipients { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IssueTokens"/> class.
        /// </summary>
        public IssueTokens()
        {
            // Initialize with default values
            Amount = BigInteger.Zero;
        }

        /// <summary>
        /// Sets the recipient.
        /// </summary>
        /// <param name="recipient">The recipient URL.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens WithRecipient(Url recipient)
        {
            Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            return this;
        }

        /// <summary>
        /// Sets the recipient.
        /// </summary>
        /// <param name="recipient">The recipient URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens WithRecipient(string recipient)
        {
            if (string.IsNullOrEmpty(recipient)) throw new ArgumentNullException(nameof(recipient));
            return WithRecipient(new Url(recipient));
        }

        /// <summary>
        /// Sets the amount.
        /// </summary>
        /// <param name="amount">The amount of tokens to issue.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens WithAmount(BigInteger amount)
        {
            Amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the amount.
        /// </summary>
        /// <param name="amount">The amount of tokens to issue.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens WithAmount(ulong amount)
        {
            Amount = new BigInteger(amount);
            return this;
        }

        /// <summary>
        /// Sets the recipients for advanced issuing.
        /// </summary>
        /// <param name="recipients">An array of token recipients.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens WithRecipients(params TokenRecipient[] recipients)
        {
            if (recipients == null) throw new ArgumentNullException(nameof(recipients));
            
            if (Recipients == null)
                Recipients = new List<TokenRecipient>();
            else
                Recipients.Clear();
                
            Recipients.AddRange(recipients);
            return this;
        }

        /// <summary>
        /// Adds a recipient for advanced issuing.
        /// </summary>
        /// <param name="url">The recipient URL.</param>
        /// <param name="amount">The amount of tokens to issue.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens AddRecipient(Url url, ulong amount)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            
            if (Recipients == null)
                Recipients = new List<TokenRecipient>();
                
            Recipients.Add(new TokenRecipient { Url = url, Amount = amount });
            return this;
        }

        /// <summary>
        /// Adds a recipient for advanced issuing.
        /// </summary>
        /// <param name="url">The recipient URL as a string.</param>
        /// <param name="amount">The amount of tokens to issue.</param>
        /// <returns>This instance for method chaining.</returns>
        public IssueTokens AddRecipient(string url, ulong amount)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return AddRecipient(new Url(url), amount);
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.IssueTokens);
            
            // Marshal recipient as field 2 if present
            if (Recipient != null)
            {
                marshaller.WriteValue(2, Recipient);
            }
            
            // Marshal amount as field 3
            if (Amount != BigInteger.Zero)
            {
                byte[] amountBytes = Amount.ToByteArray();
                marshaller.WriteBytes(3, amountBytes);
            }
            
            // Marshal recipients as field 4 if present (array of TokenRecipient)
            // Note: In the JavaScript SDK, this is a repeatable field with nested structure
            if (Recipients != null && Recipients.Count > 0)
            {
                foreach (var recipient in Recipients)
                {
                    // Each recipient is marshalled as field 4 with nested fields
                    var recipientMarshaller = new Marshaller();
                    recipientMarshaller.WriteValue(1, recipient.Url);
                    recipientMarshaller.WriteUInt(2, recipient.Amount);
                    marshaller.WriteBytes(4, recipientMarshaller.GetBytes());
                }
            }
            
            return marshaller.GetBytes();
        }
    }
} 