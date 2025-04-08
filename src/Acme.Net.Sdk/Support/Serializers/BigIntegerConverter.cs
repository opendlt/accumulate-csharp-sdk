using System;
using System.Numerics;
using Newtonsoft.Json;

namespace Acme.Net.Sdk.Support.Serializers
{
    /// <summary>
    /// JSON converter for System.Numerics.BigInteger, using string representation.
    /// </summary>
    public class BigIntegerConverter : JsonConverter<BigInteger>
    {
        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The JsonReader to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="hasExistingValue">Whether there is an existing value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override BigInteger ReadJson(JsonReader reader, Type objectType, BigInteger existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return BigInteger.Zero;

            if (reader.TokenType == JsonToken.Integer)
            {
                if (reader.Value is long longValue)
                    return new BigInteger(longValue);
                else if (reader.Value is int intValue)
                    return new BigInteger(intValue);
                else if (reader.Value is decimal decimalValue)
                    return new BigInteger(decimalValue);
            }

            if (reader.TokenType == JsonToken.String)
            {
                string stringValue = reader.Value?.ToString() ?? string.Empty;
                if (BigInteger.TryParse(stringValue, out BigInteger result))
                    return result;
            }

            throw new JsonSerializationException($"Unexpected token type: {reader.TokenType} when parsing BigInteger");
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The JsonWriter to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, BigInteger value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
} 