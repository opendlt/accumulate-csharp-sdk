using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Base interface for operations that can be applied to an Accumulate Key Page.
    /// All key page operations must be marshallable.
    /// Handles polymorphic JSON deserialization based on the 'type' property.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.protocol.KeyPageOperation.
    /// </summary>
    [JsonConverter(typeof(KeyPageOperationConverter))] // Use a custom converter for polymorphic deserialization
    public interface IKeyPageOperation : IMarshallable
    {
        // Currently empty beyond inheriting IMarshallable, serves for type hierarchy and JSON conversion.
    }

    /// <summary>
    /// Custom JsonConverter for deserializing different IKeyPageOperation implementations based on the 'type' field.
    /// </summary>
    public class KeyPageOperationConverter : JsonConverter<IKeyPageOperation>
    {
        // Dictionary to map 'type' string to the actual C# Type
        // We will populate this map as we port the specific operation classes
        private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Example entries (will be replaced with actual types later):
            // { "add", typeof(AddKeyOperation) },
            // { "update", typeof(UpdateKeyOperation) },
            // { "remove", typeof(RemoveKeyOperation) },
            // { "setThreshold", typeof(SetThresholdKeyPageOperation) },
            // { "updateAllowed", typeof(UpdateAllowedKeyPageOperation) }
        };

        // Disable writing JSON from the base interface converter; subtypes should handle their own serialization.
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IKeyPageOperation? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("KeyPageOperationConverter is only used for reading JSON.");
        }

        public override IKeyPageOperation? ReadJson(JsonReader reader, Type objectType, IKeyPageOperation? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                throw new JsonSerializationException("KeyPageOperation JSON object must have a 'type' string property.");
            }

            string operationType = typeToken.Value<string>()!;

            // Look up the corresponding C# type
            if (TypeMapping.TryGetValue(operationType, out var targetType))
            {
                // Create an instance of the target type and populate it
                using (var subReader = jsonObject.CreateReader())
                {
                    return (IKeyPageOperation?)serializer.Deserialize(subReader, targetType);
                }
            }
            else
            {
                // Handle unknown types
                throw new JsonSerializationException($"Unknown key page operation type '{operationType}'.");
            }
        }
    }
} 