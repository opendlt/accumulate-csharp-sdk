using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex class
using Acme.Net.Sdk.Commons.Codec; // For DecoderException

namespace Acme.Net.Sdk.Support.Json
{
    /// <summary>
    /// Converts a List of byte arrays (IList<byte[]?>) to and from a JSON array of lowercase hexadecimal strings (or nulls).
    /// </summary>
    public class Bytes2DHexConverter : JsonConverter<IList<byte[]?>?>
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        public override void WriteJson(JsonWriter writer, IList<byte[]?>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();
            foreach (var item in value)
            {
                if (item == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    string hexString = Hex.EncodeHexString(item, toLowerCase: true);
                    writer.WriteValue(hexString);
                }
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        public override IList<byte[]?>? ReadJson(JsonReader reader, Type objectType, IList<byte[]?>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException($"Unexpected token parsing 2D hex bytes. Expected StartArray or Null, got {reader.TokenType}.");
            }

            var list = new List<byte[]?>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    return list;
                }

                if (reader.TokenType == JsonToken.Null)
                {
                    list.Add(null);
                }
                else if (reader.TokenType == JsonToken.String)
                {
                    string? hexString = reader.Value?.ToString();
                    if (string.IsNullOrEmpty(hexString))
                    {
                        list.Add(Array.Empty<byte>()); // Consistent with BytesHexConverter
                    }
                    else
                    {
                        try
                        {
                            list.Add(Hex.DecodeHex(hexString));
                        }
                        catch (DecoderException ex)
                        {
                             throw new JsonSerializationException($"Error decoding hex string in 2D array: {hexString}", ex);
                        }
                        catch (Exception ex)
                        {
                             throw new JsonSerializationException($"Unexpected error decoding hex string in 2D array: {hexString}", ex);
                        }
                    }
                }
                else
                {
                     throw new JsonSerializationException($"Unexpected token inside JSON array for 2D hex bytes. Expected String or Null, got {reader.TokenType}.");
                }
            }

            // Should have returned when EndArray was found.
            throw new JsonSerializationException("Unexpected end of JSON data while reading 2D hex bytes array.");
        }
    }
}
