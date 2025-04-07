using System;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a transaction fee in Accumulate.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.Fee.
    /// Serializes/deserializes as an unwrapped BigInteger value in JSON.
    /// </summary>
    [JsonConverter(typeof(FeeConverter))] 
    public class Fee
    {
        /// <summary>
        /// Gets or sets the fee value.
        /// </summary>
        public BigInteger Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fee"/> class with a default value of zero.
        /// </summary>
        public Fee()
        {
            Value = BigInteger.Zero;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fee"/> class with the specified value.
        /// </summary>
        /// <param name="value">The fee value.</param>
        public Fee(BigInteger value)
        {
            Value = value;
        }

        // Implicit conversion for convenience (optional)
        public static implicit operator BigInteger(Fee fee) => fee.Value;
        public static implicit operator Fee(BigInteger value) => new Fee(value);

        public override string ToString() => Value.ToString();

        public override bool Equals(object? obj)
        {
            return obj is Fee fee && Value.Equals(fee.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    /// <summary>
    /// Custom JsonConverter for Fee serialization/deserialization as an unwrapped BigInteger.
    /// </summary>
    public class FeeConverter : JsonConverter<Fee>
    {
        public override void WriteJson(JsonWriter writer, Fee? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                // Write the BigInteger value directly
                // JToken.FromObject will handle BigInteger serialization (likely as string or number depending on size/settings)
                JToken token = JToken.FromObject(value.Value, serializer);
                token.WriteTo(writer);
            }
        }

        public override Fee? ReadJson(JsonReader reader, Type objectType, Fee? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            // Read the token directly (expecting Integer or String representation of BigInteger)
            JToken token = JToken.Load(reader);
            try
            {
                // Deserialize the token as BigInteger
                BigInteger feeValue = token.ToObject<BigInteger>(serializer);
                return new Fee(feeValue);
            }
            catch (Exception ex) when (ex is JsonSerializationException || ex is FormatException || ex is ArgumentException)
            {
                throw new JsonSerializationException($"Error deserializing Fee from token '{token}'. Expected BigInteger representation.", ex);
            }
        }
    }
} 