using System;
using System.Numerics;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for burning tokens.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class BurnTokens : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "burnTokens";

        /// <summary>
        /// Gets or sets the amount of tokens to burn.
        /// </summary>
        [JsonProperty("amount")]
        public BigInteger Amount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BurnTokens"/> class.
        /// </summary>
        public BurnTokens()
        {
            // Initialize with default values
            Amount = BigInteger.Zero;
        }

        /// <summary>
        /// Sets the amount of tokens to burn.
        /// </summary>
        /// <param name="amount">The amount as a BigInteger.</param>
        /// <returns>This instance for method chaining.</returns>
        public BurnTokens WithAmount(BigInteger amount)
        {
            if (amount < BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
                
            Amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the amount of tokens to burn.
        /// </summary>
        /// <param name="amount">The amount as a long.</param>
        /// <returns>This instance for method chaining.</returns>
        public BurnTokens WithAmount(long amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
                
            Amount = new BigInteger(amount);
            return this;
        }

        /// <summary>
        /// Sets the amount of tokens to burn.
        /// </summary>
        /// <param name="amount">The amount as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if amount is null.</exception>
        /// <exception cref="FormatException">Thrown if amount is not a valid number.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public BurnTokens WithAmount(string amount)
        {
            if (string.IsNullOrEmpty(amount))
                throw new ArgumentNullException(nameof(amount));
                
            var value = BigInteger.Parse(amount);
            return WithAmount(value);
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.BurnTokens);
            
            // Marshal Amount as field 2
            marshaller.WriteValue(2, Amount.ToByteArray());
            
            return marshaller.GetBytes();
        }
    }
} 