using System;
using System.Numerics;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for adding credits to an account.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class AddCredits : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "addCredits";

        /// <summary>
        /// Gets or sets the URL of the recipient account.
        /// </summary>
        [JsonProperty("recipient")]
        public Url? Recipient { get; set; }

        /// <summary>
        /// Gets or sets the amount of credits to add.
        /// </summary>
        [JsonProperty("amount")]
        public BigInteger Amount { get; set; } = BigInteger.Zero;

        /// <summary>
        /// Gets or sets the oracle signature (if required).
        /// </summary>
        [JsonProperty("oracle")]
        public string? Oracle { get; set; }

        /// <summary>
        /// Sets the recipient account URL.
        /// </summary>
        /// <param name="recipient">The recipient account URL.</param>
        /// <returns>This instance for method chaining.</returns>
        public AddCredits WithRecipient(Url recipient)
        {
            Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            return this;
        }

        /// <summary>
        /// Sets the recipient account URL.
        /// </summary>
        /// <param name="recipient">The recipient account URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        public AddCredits WithRecipient(string recipient)
        {
            if (string.IsNullOrEmpty(recipient)) throw new ArgumentNullException(nameof(recipient));
            return WithRecipient(new Url(recipient));
        }

        /// <summary>
        /// Sets the amount of credits to add.
        /// </summary>
        /// <param name="amount">The amount of credits as a BigInteger.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public AddCredits WithAmount(BigInteger amount)
        {
            if (amount < BigInteger.Zero) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
            Amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the amount of credits to add.
        /// </summary>
        /// <param name="amount">The amount of credits as a long.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public AddCredits WithAmount(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
            return WithAmount(new BigInteger(amount));
        }

        /// <summary>
        /// Sets the oracle signature.
        /// </summary>
        /// <param name="oracle">The oracle signature.</param>
        /// <returns>This instance for method chaining.</returns>
        public AddCredits WithOracle(string oracle)
        {
            Oracle = oracle ?? throw new ArgumentNullException(nameof(oracle));
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.AddCredits);

            // Marshal recipient URL as field 2
            if (Recipient != null)
            {
                marshaller.WriteValue(2, Recipient);
            }

            // Marshal amount as field 3 - convert to byte array directly
            if (Amount > BigInteger.Zero)
            {
                byte[] amountBytes = Amount.ToByteArray();
                marshaller.WriteBytes(3, amountBytes);
            }
            else
            {
                // Write empty byte array for zero
                marshaller.WriteBytes(3, new byte[] { 0 });
            }

            // Marshal oracle as field 4 if present
            // Note: The JavaScript SDK shows this as uint, but we're using string
            if (!string.IsNullOrEmpty(Oracle))
            {
                marshaller.WriteString(4, Oracle);
            }

            return marshaller.GetBytes();
        }
    }
} 