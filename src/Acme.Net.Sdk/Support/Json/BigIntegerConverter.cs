using System;
using System.Numerics;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Acme.Net.Sdk.Support.Json
{
    /// <summary>
    /// Converts a System.Numerics.BigInteger to and from a JSON string representation.
    /// Handles potential number input during deserialization for robustness.
    /// </summary>
    public class BigIntegerConverter : JsonConverter<BigInteger>
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        public override void WriteJson(JsonWriter writer, BigInteger value, JsonSerializer serializer)
        {
            // Always write BigInteger as a string
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        public override BigInteger ReadJson(JsonReader reader, Type objectType, BigInteger existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            if (token.Type == JTokenType.Null)
            {
                // Decide handling: throw or return default(BigInteger)? Let's return default.
                 return default(BigInteger); 
            }

            if (token.Type == JTokenType.String)
            {
                string? s = token.Value<string>();
                 if (string.IsNullOrEmpty(s))
                 {
                     return default(BigInteger);
                 }
                try
                {
                    return BigInteger.Parse(s, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException($"Error parsing BigInteger string: {s}", ex);
                }
            }
            
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) // Handle number input as well
            {
                 try
                 {
                    // Use JToken's conversion which handles various number formats
                    return token.Value<BigInteger>(); 
                 }
                 catch (Exception ex)
                 {
                    throw new JsonSerializationException($"Error converting number token to BigInteger: {token.ToString()}", ex);
                 }
            }

            throw new JsonSerializationException($"Unexpected token parsing BigInteger. Expected String, Integer, Float or Null, got {token.Type}.");
        }
    }
}
