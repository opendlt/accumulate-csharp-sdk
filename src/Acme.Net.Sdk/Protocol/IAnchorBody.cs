using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Base interface for Accumulate anchor transaction bodies.
    /// Handles polymorphic JSON deserialization based on the 'type' property.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.protocol.AnchorBody.
    /// </summary>
    [JsonConverter(typeof(AnchorBodyConverter))] // Use a custom converter for polymorphic deserialization
    public interface IAnchorBody
    {
        // Currently empty, serves as a marker interface for type hierarchy and JSON conversion.
    }

    /// <summary>
    /// Custom JsonConverter for deserializing different IAnchorBody implementations based on the 'type' field.
    /// </summary>
    public class AnchorBodyConverter : JsonConverter<IAnchorBody>
    {
        // Dictionary to map 'type' string to the actual C# Type
        // We will populate this map as we port the specific anchor body classes
        private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Example entries (will be replaced with actual types later):
            // { "blockValidatorAnchor", typeof(BlockValidatorAnchor) },
            // { "directoryAnchor", typeof(DirectoryAnchor) }
        };

        // Disable writing JSON from the base interface converter; subtypes should handle their own serialization.
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IAnchorBody? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("AnchorBodyConverter is only used for reading JSON.");
        }

        public override IAnchorBody? ReadJson(JsonReader reader, Type objectType, IAnchorBody? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject jsonObject = JObject.Load(reader);

            // Extract the 'type' property
            var typeToken = jsonObject["type"];
            if (typeToken == null || typeToken.Type != JTokenType.String)
            {
                throw new JsonSerializationException("AnchorBody JSON object must have a 'type' string property.");
            }

            string anchorType = typeToken.Value<string>()!;

            // Look up the corresponding C# type
            if (TypeMapping.TryGetValue(anchorType, out var targetType))
            {
                // Create an instance of the target type and populate it
                using (var subReader = jsonObject.CreateReader())
                {
                    return (IAnchorBody?)serializer.Deserialize(subReader, targetType);
                }
            }
            else
            {
                // Handle unknown types
                throw new JsonSerializationException($"Unknown anchor body type '{anchorType}'.");
            }
        }
    }
}

