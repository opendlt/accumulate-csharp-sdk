using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // For JToken

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Provides helper methods for deserializing JSON results, particularly API responses.
    /// </summary>
    public static class ResultReader
    {
        // Configure JsonSerializerSettings similarly to the Java ObjectMapper setup
        // Newtonsoft.Json is generally forgiving of extra fields (FAIL_ON_UNKNOWN_PROPERTIES=false is default)
        // FAIL_ON_MISSING_EXTERNAL_TYPE_ID_PROPERTY doesn't have a direct equivalent but is less common in Json.NET usage.
        // JavaTimeModule equivalent: Json.NET handles DateTime/DateTimeOffset well by default, customize if needed.
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            // Example: Configure DateTime handling if necessary
            // DateParseHandling = DateParseHandling.DateTimeOffset,
            // DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            
            // Add converters if needed, e.g., for specific JavaTime types
            // Converters = new List<JsonConverter> { new IsoDateTimeConverter() },

            // MissingMemberHandling = MissingMemberHandling.Ignore // Default
        };

        /// <summary>
        /// Deserializes a JSON string into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <param name="valueType">The target type.</param> // Note: Redundant if T is used
        /// <returns>The deserialized object.</returns>
        /// <exception cref="Newtonsoft.Json.JsonException">Thrown if deserialization fails.</exception>
        public static T ReadValue<T>(string json) // Simplified signature using generic T
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, SerializerSettings)!;
            }
            catch (Newtonsoft.Json.JsonException) // Fully qualified JsonException
            {
                throw; 
            }
        }
        
        /// <summary>
        /// Deserializes a JSON string into an object of the specified type.
        /// (Overload matching Java signature with Type parameter)
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="valueType">The target type.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="Newtonsoft.Json.JsonException">Thrown if deserialization fails.</exception>
        public static object ReadValue(string json, Type valueType)
        {
             try
            {
                return JsonConvert.DeserializeObject(json, valueType, SerializerSettings)!;
            }
            catch (Newtonsoft.Json.JsonException) // Fully qualified JsonException
            {
                throw; 
            }
        }

        /// <summary>
        /// Converts a JToken (JSON node) into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="node">The JToken node.</param>
        /// <returns>The converted object.</returns>
        /// <exception cref="Newtonsoft.Json.JsonException">Thrown if conversion fails.</exception>
        public static T ReadValue<T>(JToken node) // Changed parameter type to JToken
        {
            try
            {
                return node.ToObject<T>(Newtonsoft.Json.JsonSerializer.Create(SerializerSettings))!;
            }
            catch (Newtonsoft.Json.JsonException) // Fully qualified JsonException
            {
                throw;
            }
        }
        
         /// <summary>
        /// Converts a JToken (JSON node) into an object of the specified type.
        /// (Overload matching Java signature with Type parameter)
        /// </summary>
        /// <param name="node">The JToken node.</param>
        /// <param name="valueType">The target type.</param>
        /// <returns>The converted object.</returns>
        /// <exception cref="Newtonsoft.Json.JsonException">Thrown if conversion fails.</exception>
        public static object ReadValue(JToken node, Type valueType) // Changed parameter type to JToken
        {
            try
            {
                return node.ToObject(valueType, Newtonsoft.Json.JsonSerializer.Create(SerializerSettings))!;
            }
            catch (Newtonsoft.Json.JsonException) // Fully qualified JsonException
            {
                throw;
            }
        }

        /// <summary>
        /// Converts a JToken (representing a JSON array) into a List of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="node">The JToken node (should be an array).</param>
        /// <returns>A list containing the converted elements.</returns>
        /// <exception cref="Newtonsoft.Json.JsonException">Thrown if conversion fails.</exception>
        public static List<T> ReadList<T>(JToken node)
        {
            try
            {
                return node.ToObject<List<T>>(Newtonsoft.Json.JsonSerializer.Create(SerializerSettings))!;
            }
            catch (Newtonsoft.Json.JsonException) // Fully qualified JsonException
            {
                throw;
            }
        }

        // --- Stubbed methods depending on generated classes --- 

        /// <summary>
        /// Reads a MultiResponse structure from a JToken.
        /// </summary>
        /// <typeparam name="T">The type of items in the MultiResponse.</typeparam>
        /// <param name="node">The JToken representing the MultiResponse.</param>
        /// <param name="itemType">The type of the items.</param>
        /// <returns>A deserialized MultiResponse object.</returns>
        /// <exception cref="NotImplementedException">Thrown because dependent generated classes are not yet ported.</exception>
        /// <remarks>TODO: Implement fully when generated classes (MultiResponse) are ported.</remarks>
        public static object /* MultiResponse<T> */ ReadMultiResponse<T>(JToken node, Type itemType)
        {
            // Original logic:
            // final MultiResponse multiResponse = readValue(node, MultiResponse.class);
            // final List<T> items = objectMapper.convertValue(multiResponse.getItems(),
            //         objectMapper.getTypeFactory().constructCollectionType(List.class, aClass));
            // final List<JsonNode> otherItems = ...
            // return new io.accumulatenetwork.sdk.protocol.MultiResponse(items, otherItems, ...);
            throw new NotImplementedException("Depends on generated MultiResponse class.");
        }

        /// <summary>
        /// Checks a TxResponse for errors.
        /// </summary>
        /// <param name="txResponse">The transaction response object.</param>
        /// <exception cref="NotImplementedException">Thrown because dependent generated classes are not yet ported.</exception>
        /// <exception cref="ApplicationException">Thrown if the response indicates an error.</exception>
        /// <remarks>TODO: Implement fully when generated classes (TxResponse) are ported.</remarks>
        public static void CheckForErrors(object /* TxResponse */ txResponse)
        {
            throw new NotImplementedException("Depends on generated TxResponse class.");
            // Example logic:
            // if (txResponse == null) throw new ApplicationException("No transaction response");
            // if (txResponse.Code != 0) throw new ApplicationException($"Transaction error: {txResponse.Code} - {txResponse.Message}");
        }

        /// <summary>
        /// Checks a TxResponse and TransactionStatus for errors.
        /// </summary>
        /// <param name="txResponse">The transaction response object.</param>
        /// <param name="txStatus">The transaction status object.</param>
        /// <exception cref="NotImplementedException">Thrown because dependent generated classes are not yet ported.</exception>
        /// <exception cref="ApplicationException">Thrown if the response or status indicates an error.</exception>
        /// <remarks>TODO: Implement fully when generated classes (TxResponse, TransactionStatus) are ported.</remarks>
        public static void CheckForErrors(object /* TxResponse */ txResponse, object? /* TransactionStatus */ txStatus)
        {
            throw new NotImplementedException("Depends on generated TxResponse and TransactionStatus classes.");
            // Complex logic involving checking both objects and potentially combining error messages
        }

        /// <summary>
        /// Determines the TransactionType from a JToken.
        /// </summary>
        /// <param name="jsonNode">The JToken node.</param>
        /// <returns>The determined TransactionType.</returns>
        /// <exception cref="NotImplementedException">Thrown because dependent generated classes are not yet ported.</exception>
        /// <remarks>TODO: Implement fully when generated classes (TransactionType) are ported.</remarks>
        public static object /* TransactionType */ GetTransactionType(JToken jsonNode)
        {
            // Logic involves checking if node is text/number or object with specific fields ("type", "from"/"to")
            // Then calls TransactionType.fromJsonNode(node) or similar.
            throw new NotImplementedException("Depends on generated TransactionType enum/class.");
        }

        /// <summary>
        /// Gets the account type from a JSON node.
        /// </summary>
        /// <param name="jsonNode">The JSON node containing the account type.</param>
        /// <returns>The account type.</returns>
        public static object /* AccountType */ GetAccountType(JToken jsonNode)
        {
            // This is a placeholder implementation.
            // In a real implementation, it would parse the JSON and return the account type.
            throw new NotImplementedException("Depends on generated AccountType enum/class.");
        }

        /// <summary>
        /// Deserializes a System.Text.Json.JsonElement to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="element">The JsonElement to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentException">Thrown when deserialization fails.</exception>
        public static T ReadValue<T>(System.Text.Json.JsonElement element)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(element.GetRawText())
                    ?? throw new ArgumentException($"Failed to deserialize JsonElement to {typeof(T).Name}", nameof(element));
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new ArgumentException($"Failed to deserialize JsonElement to {typeof(T).Name}", nameof(element), ex);
            }
        }

        /// <summary>
        /// Checks for errors in the transaction response.
        /// </summary>
        /// <param name="txResponse">The transaction response to check.</param>
        public static void CheckForErrors(Api.V2.TxResponse txResponse)
        {
            // This is a placeholder implementation.
            // In a real implementation, it would check for error codes and throw appropriate exceptions.
        }

        /// <summary>
        /// Checks for errors in the transaction response and status.
        /// </summary>
        /// <param name="txResponse">The transaction response to check.</param>
        /// <param name="transactionStatus">The transaction status to check.</param>
        public static void CheckForErrors(Api.V2.TxResponse txResponse, object transactionStatus)
        {
            // This is a placeholder implementation.
            // In a real implementation, it would check for error codes and throw appropriate exceptions.
        }
    }
} 