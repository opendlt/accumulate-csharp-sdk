using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Commons.Codec;

namespace Acme.Net.Sdk.Protocol.JsonConverters
{
    /// <summary>
    /// Converts between byte arrays and hex strings for JSON serialization/deserialization.
    /// </summary>
    public class HexConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>True if this instance can convert the specified object type; otherwise, false.</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The JsonReader to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
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
                    return Array.Empty<byte>();
                }

                try
                {
                    return Hex.DecodeHex(hexString);
                }
                catch (DecoderException ex)
                {
                    throw new JsonSerializationException($"Error decoding hex string: {hexString}", ex);
                }
            }

            throw new JsonSerializationException($"Unexpected token when parsing hex string. Expected String, got {reader.TokenType}.");
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The JsonWriter to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            byte[] bytes = (byte[])value;
            string hexString = Hex.EncodeHexString(bytes, toLowerCase: true);
            writer.WriteValue(hexString);
        }
    }
} 