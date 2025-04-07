using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters; // For StringEnumConverter

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents the type of response expected from a query.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))] // Use string representation in JSON
    public enum QueryResponseType
    {
        /// <summary>
        /// Key Page Index Response.
        /// </summary>
        [EnumMember(Value = "key-page-index")]
        KeyPageIndex = 1, // Assigning 1, Java uses ordinal() in fromJsonNode sometimes, but constructor implies value. Needs clarification.

        // Add other types as they are identified or ported.
    }

    public static class QueryResponseTypeExtensions
    {
        // Store associated type information (using placeholders initially)
        // TODO: Replace placeholder types with actual generated types when available
        private static readonly Dictionary<QueryResponseType, Type> ResponseTypeMap = new Dictionary<QueryResponseType, Type>
        {
            { QueryResponseType.KeyPageIndex, typeof(object) /* Replace with typeof(Generated.ResponseKeyPageIndex) */ }
        };
        
        /// <summary>
        /// Gets the API name (string representation) associated with the QueryResponseType.
        /// Uses the EnumMember attribute value.
        /// </summary>
        /// <param name="type">The query response type.</param>
        /// <returns>The API name string.</returns>
        public static string GetResponseTypeString(this QueryResponseType type)
        {
            var enumType = typeof(QueryResponseType);
            var memberInfo = enumType.GetMember(type.ToString());
            var enumMemberAttribute = memberInfo.FirstOrDefault()?.
                GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .OfType<EnumMemberAttribute>()
                .FirstOrDefault();
            
            return enumMemberAttribute?.Value ?? type.ToString().ToLowerInvariant(); // Fallback logic
        }
        
        /// <summary>
        /// Gets the corresponding response class (Type) for the QueryResponseType.
        /// </summary>
        /// <param name="type">The query response type.</param>
        /// <returns>The .NET Type of the response class.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the type is not mapped.</exception>
        public static Type GetResponseClass(this QueryResponseType type)
        {
             if (ResponseTypeMap.TryGetValue(type, out var responseClass))
            {
                return responseClass;
            }
            // Should not happen if all enum members are mapped
            throw new KeyNotFoundException($"Response class type not found for QueryResponseType: {type}");
        }
        
        /// <summary>
        /// Finds the QueryResponseType associated with a given response class Type.
        /// </summary>
        /// <param name="responseClass">The .NET Type of the response class.</param>
        /// <returns>The corresponding QueryResponseType.</returns>
        /// <exception cref="ArgumentException">Thrown if no QueryResponseType matches the given class type.</exception>
        public static QueryResponseType FromClass(Type responseClass)
        {
            foreach (var kvp in ResponseTypeMap)
            {
                // TODO: This check might need adjustment once actual generated types are used.
                // Comparing typeof(object) placeholder won't work correctly.
                if (kvp.Value == responseClass || kvp.Value.IsAssignableFrom(responseClass)) 
                {
                    return kvp.Key;
                }
            }
            throw new ArgumentException($"No Query Response Type found for class {responseClass.FullName}");
        }
        
        // Note: The custom `fromJsonNode` static method from Java is complex.
        // Basic string conversion is handled by StringEnumConverter.
        // Handling conversion from number or nested 'type' property would require
        // a custom JsonConverter for QueryResponseType.
        // For now, relying on StringEnumConverter.
    }
} 