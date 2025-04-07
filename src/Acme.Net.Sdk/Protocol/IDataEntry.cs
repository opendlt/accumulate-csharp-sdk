using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Base interface for data entries within Accumulate transactions.
    /// All data entries must be marshallable.
    /// Handles polymorphic JSON deserialization based on the 'type' property.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.protocol.DataEntry.
    /// </summary>
    [JsonConverter(typeof(DataEntryConverter))] // Use a custom converter for polymorphic deserialization
    public interface IDataEntry : IMarshallable
    {
        // Currently empty beyond inheriting IMarshallable, serves for type hierarchy and JSON conversion.
    }

    /// <summary>
    /// Custom JsonConverter for deserializing different IDataEntry implementations based on the 'type' field.
    /// </summary>
    public class DataEntryConverter : JsonConverter<IDataEntry>
    {
        // Dictionary to map 'type' string to the actual C# Type
        // We will populate this map as we port the specific entry classes
        private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Example entries (will be replaced with actual types later):
            // { "accumulate", typeof(AccumulateDataEntry) },
            // { "factom", typeof(FactomDataEntry) }
        };

        // Disable writing JSON from the base interface converter; subtypes should handle their own serialization.
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IDataEntry? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("DataEntryConverter is only used for reading JSON.");
        }

        public override IDataEntry? ReadJson(JsonReader reader, Type objectType, IDataEntry? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                throw new JsonSerializationException("DataEntry JSON object must have a 'type' string property.");
            }

            string entryType = typeToken.Value<string>()!;

            // Look up the corresponding C# type
            if (TypeMapping.TryGetValue(entryType, out var targetType))
            {
                // Create an instance of the target type and populate it
                using (var subReader = jsonObject.CreateReader())
                {
                    return (IDataEntry?)serializer.Deserialize(subReader, targetType);
                }
            }
            else
            {
                // Handle unknown types
                throw new JsonSerializationException($"Unknown data entry type '{entryType}'.");
            }
        }
    }
}

