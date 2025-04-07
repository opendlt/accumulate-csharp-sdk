using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex class
using Acme.Net.Sdk.Commons.Codec; // For DecoderException

namespace Acme.Net.Sdk.Support.Json
{
    /// <summary>
    /// Converts a byte array to and from a lowercase hexadecimal JSON string.
    /// </summary>
    public class BytesHexConverter : JsonConverter<byte[]?>
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            string hexString = Hex.EncodeHexString(value, toLowerCase: true);
            writer.WriteValue(hexString);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                string? hexString = reader.Value?.ToString();
                if (string.IsNullOrEmpty(hexString))
                {
                    // Decide handling: empty array or null? Let's return empty array for empty string, null for null token.
                    return Array.Empty<byte>(); 
                }

                try
                {
                    return Hex.DecodeHex(hexString);
                }
                catch (DecoderException ex) // Catch specific exception from our Hex helper
                {
                    throw new JsonSerializationException($"Error decoding hex string: {hexString}", ex);
                }
                catch (Exception ex) // Catch unexpected errors during decode
                {
                     throw new JsonSerializationException($"Unexpected error decoding hex string: {hexString}", ex);
                }
            }

            throw new JsonSerializationException($"Unexpected token parsing hex bytes. Expected String or Null, got {reader.TokenType}.");
        }
    }
}
