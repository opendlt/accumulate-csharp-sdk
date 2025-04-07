using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Base marker interface for the result of transaction execution.
    /// Handles polymorphic JSON deserialization based on the 'type' property.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.protocol.TransactionResult.
    /// </summary>
    [JsonConverter(typeof(TransactionResultConverter))] // Use a custom converter for polymorphic deserialization
    public interface ITransactionResult
    {
        // Currently empty, serves as a marker interface for type hierarchy and JSON conversion.
    }

    /// <summary>
    /// Custom JsonConverter for deserializing different ITransactionResult implementations based on the 'type' field.
    /// </summary>
    public class TransactionResultConverter : JsonConverter<ITransactionResult>
    {
        // Dictionary to map 'type' string to the actual C# Type
        // We will populate this map as we port the specific result classes
        private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Example entries (will be replaced with actual types later):
            // { "addCredits", typeof(AddCreditsResult) },
            // { "writeData", typeof(WriteDataResult) },
            // { "unknown", typeof(EmptyResult) } // Note: Java used "unknown" for EmptyResult
        };

        // Disable writing JSON from the base interface converter; subtypes should handle their own serialization.
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, ITransactionResult? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("TransactionResultConverter is only used for reading JSON.");
        }

        public override ITransactionResult? ReadJson(JsonReader reader, Type objectType, ITransactionResult? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                throw new JsonSerializationException("TransactionResult JSON object must have a 'type' string property.");
            }

            string resultType = typeToken.Value<string>()!;

            // Look up the corresponding C# type
            if (TypeMapping.TryGetValue(resultType, out var targetType))
            {
                // Create an instance of the target type and populate it
                using (var subReader = jsonObject.CreateReader())
                {
                    return (ITransactionResult?)serializer.Deserialize(subReader, targetType);
                }
            }
            else
            {
                // Handle unknown types - Java mapped "unknown" to EmptyResult. If that's not in the map explicitly,
                // we might default to an EmptyResult type if it exists, or throw.
                 // For now, throw if not explicitly mapped.
                 throw new JsonSerializationException($"Unknown transaction result type '{resultType}'.");
                 // TODO: Consider adding default mapping to EmptyResult if appropriate after it's ported.
            }
        }
    }
}

