using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Base interface for all Accumulate account types.
    /// Defines the common property for an account's URL.
    /// Handles polymorphic JSON deserialization based on the 'type' property.
    /// </summary>
    [JsonConverter(typeof(AccountConverter))] // Use a custom converter for polymorphic deserialization
    public interface IAccount
    {
        /// <summary>
        /// Gets the URL of the account.
        /// </summary>
        Url Url { get; }
    }

    /// <summary>
    /// Custom JsonConverter for deserializing different IAccount implementations based on the 'type' field.
    /// </summary>
    public class AccountConverter : JsonConverter<IAccount>
    {
        // Dictionary to map 'type' string to the actual C# Type
        // We will populate this map as we port the specific account classes (ADI, TokenAccount, etc.)
        private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Example entries (will be replaced with actual types later):
            // { "unknown", typeof(UnknownAccount) },
            // { "identity", typeof(ADI) },
            // { "tokenIssuer", typeof(TokenIssuer) },
            // { "tokenAccount", typeof(TokenAccount) },
            // { "liteTokenAccount", typeof(LiteTokenAccount) },
            // { "keyPage", typeof(KeyPage) },
            // { "keyBook", typeof(KeyBook) },
            // { "dataAccount", typeof(DataAccount) },
            // { "liteDataAccount", typeof(LiteDataAccount) },
            // { "unknownSigner", typeof(UnknownSigner) },
            // { "liteIdentity", typeof(LiteIdentity) }
        };

        // Disable writing JSON from the base interface converter; subtypes should handle their own serialization.
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IAccount? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("AccountConverter is only used for reading JSON.");
        }

        public override IAccount? ReadJson(JsonReader reader, Type objectType, IAccount? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                throw new JsonSerializationException("Account JSON object must have a 'type' string property.");
            }

            string accountType = typeToken.Value<string>()!;

            // Look up the corresponding C# type
            if (TypeMapping.TryGetValue(accountType, out var targetType))
            {
                // Create an instance of the target type and populate it
                 // Using CreateReader() allows the default serializer or other converters to handle population
                using (var subReader = jsonObject.CreateReader())
                {
                    return (IAccount?)serializer.Deserialize(subReader, targetType);
                }
            }
            else
            {
                // Handle unknown types - maybe map to a specific UnknownAccount type or throw?
                // For now, let's throw an exception. Consider adding an UnknownAccount type later if needed.
                throw new JsonSerializationException($"Unknown account type '{accountType}'.");
                // Alternatively, deserialize as a base or specific UnknownAccount type if defined:
                // using (var subReader = jsonObject.CreateReader())
                // {
                //    return (IAccount?)serializer.Deserialize(subReader, typeof(UnknownAccount));
                // }
            }
        }
    }
}
