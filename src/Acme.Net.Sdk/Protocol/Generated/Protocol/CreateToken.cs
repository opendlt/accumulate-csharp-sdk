using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for creating a new token.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class CreateToken : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "createToken";

        /// <summary>
        /// Gets or sets the URL for the new token.
        /// </summary>
        [JsonProperty("url")]
        public Url? Url { get; set; }

        /// <summary>
        /// Gets or sets the token symbol.
        /// </summary>
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }

        /// <summary>
        /// Gets or sets the token precision.
        /// </summary>
        [JsonProperty("precision")]
        public int Precision { get; set; } = 8;

        /// <summary>
        /// Gets or sets the supply limit for the token.
        /// </summary>
        [JsonProperty("supplyLimit")]
        public ulong? SupplyLimit { get; set; }

        /// <summary>
        /// Gets or sets the properties URL for the token.
        /// </summary>
        [JsonProperty("properties")]
        public Url? Properties { get; set; }

        /// <summary>
        /// Sets the URL for the new token.
        /// </summary>
        /// <param name="url">The URL for the token.</param>
        /// <returns>This instance for method chaining.</returns>
        public CreateToken WithUrl(Url url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            return this;
        }

        /// <summary>
        /// Sets the URL for the new token.
        /// </summary>
        /// <param name="url">The URL for the token as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        public CreateToken WithUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return WithUrl(new Url(url));
        }

        /// <summary>
        /// Sets the token symbol.
        /// </summary>
        /// <param name="symbol">The token symbol.</param>
        /// <returns>This instance for method chaining.</returns>
        public CreateToken WithSymbol(string symbol)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            return this;
        }

        /// <summary>
        /// Sets the token precision.
        /// </summary>
        /// <param name="precision">The token precision.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if precision is negative.</exception>
        public CreateToken WithPrecision(int precision)
        {
            if (precision < 0) throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be non-negative");
            Precision = precision;
            return this;
        }

        /// <summary>
        /// Sets the supply limit for the token.
        /// </summary>
        /// <param name="supplyLimit">The supply limit for the token.</param>
        /// <returns>This instance for method chaining.</returns>
        public CreateToken WithSupplyLimit(ulong supplyLimit)
        {
            SupplyLimit = supplyLimit;
            return this;
        }

        /// <summary>
        /// Sets the properties URL.
        /// </summary>
        /// <param name="properties">The properties URL.</param>
        /// <returns>This instance for method chaining.</returns>
        public CreateToken WithProperties(Url properties)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            return this;
        }

        /// <summary>
        /// Sets the properties URL.
        /// </summary>
        /// <param name="properties">The properties URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        public CreateToken WithProperties(string properties)
        {
            if (string.IsNullOrEmpty(properties)) throw new ArgumentNullException(nameof(properties));
            Properties = new Url(properties);
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.CreateToken);

            // Marshal URL as field 2
            if (Url != null)
            {
                marshaller.WriteValue(2, Url);
            }

            // Marshal symbol as field 4 (skips field 3)
            if (!string.IsNullOrEmpty(Symbol))
            {
                marshaller.WriteString(4, Symbol);
            }

            // Marshal precision as field 5
            marshaller.WriteUInt(5, Precision);

            // Marshal properties as field 6 if present
            if (Properties != null)
            {
                marshaller.WriteValue(6, Properties);
            }

            // Marshal supply limit as field 7 if present (as BigInt)
            if (SupplyLimit.HasValue)
            {
                var supplyBytes = new System.Numerics.BigInteger(SupplyLimit.Value).ToByteArray();
                // Convert to big-endian
                bool hasSignByte = supplyBytes.Length > 1 && supplyBytes[supplyBytes.Length - 1] == 0;
                int length = hasSignByte ? supplyBytes.Length - 1 : supplyBytes.Length;
                var result = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    result[i] = supplyBytes[length - 1 - i];
                }
                marshaller.WriteBytes(7, result);
            }

            // Note: Field 9 would be authorities (repeatable URL array) but not implemented yet

            return marshaller.GetBytes();
        }
    }
} 