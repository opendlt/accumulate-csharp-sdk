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
        KeyPageIndex = 1,

        /// <summary>
        /// Account Response.
        /// </summary>
        [EnumMember(Value = "account")]
        Account = 2,

        /// <summary>
        /// Token Account Response.
        /// </summary>
        [EnumMember(Value = "token-account")]
        TokenAccount = 3,

        /// <summary>
        /// Token Issuer Response.
        /// </summary>
        [EnumMember(Value = "token-issuer")]
        TokenIssuer = 4,

        /// <summary>
        /// Key Book Response.
        /// </summary>
        [EnumMember(Value = "key-book")]
        KeyBook = 5,

        /// <summary>
        /// Key Page Response.
        /// </summary>
        [EnumMember(Value = "key-page")]
        KeyPage = 6,

        /// <summary>
        /// Data Account Response.
        /// </summary>
        [EnumMember(Value = "data-account")]
        DataAccount = 7,

        /// <summary>
        /// Data Entry Response.
        /// </summary>
        [EnumMember(Value = "data-entry")]
        DataEntry = 8,

        /// <summary>
        /// Transaction Response.
        /// </summary>
        [EnumMember(Value = "transaction")]
        Transaction = 9,

        /// <summary>
        /// Chain Response.
        /// </summary>
        [EnumMember(Value = "chain")]
        Chain = 10,

        /// <summary>
        /// Directory Response.
        /// </summary>
        [EnumMember(Value = "directory")]
        Directory = 11,

        /// <summary>
        /// Major Block Response.
        /// </summary>
        [EnumMember(Value = "major-block")]
        MajorBlock = 12,

        /// <summary>
        /// Network Response.
        /// </summary>
        [EnumMember(Value = "network")]
        Network = 13,

        /// <summary>
        /// Partition Response.
        /// </summary>
        [EnumMember(Value = "partition")]
        Partition = 14,

        /// <summary>
        /// Version Response.
        /// </summary>
        [EnumMember(Value = "version")]
        Version = 15,

        /// <summary>
        /// Receipt Response.
        /// </summary>
        [EnumMember(Value = "receipt")]
        Receipt = 16,

        /// <summary>
        /// Error Response.
        /// </summary>
        [EnumMember(Value = "error")]
        Error = 17
    }

    public static class QueryResponseTypeExtensions
    {
        // Store associated type information (using placeholders initially)
        // TODO: Replace placeholder types with actual generated types when available
        private static readonly Dictionary<QueryResponseType, Type> ResponseTypeMap = new Dictionary<QueryResponseType, Type>
        {
            { QueryResponseType.KeyPageIndex, typeof(object) /* Replace with typeof(Generated.ResponseKeyPageIndex) */ },
            { QueryResponseType.Account, typeof(Api.V2.AccountResponse) },
            { QueryResponseType.TokenAccount, typeof(Api.V2.AccountResponse) },
            { QueryResponseType.TokenIssuer, typeof(Api.V2.TokenResponse) },
            { QueryResponseType.KeyBook, typeof(Api.V2.AccountResponse) },
            { QueryResponseType.KeyPage, typeof(Api.V2.AccountResponse) },
            { QueryResponseType.DataAccount, typeof(Api.V2.AccountResponse) },
            { QueryResponseType.DataEntry, typeof(object) /* Replace with actual type */ },
            { QueryResponseType.Transaction, typeof(Api.V2.TransactionResponse) },
            { QueryResponseType.Chain, typeof(object) /* Replace with actual type */ },
            { QueryResponseType.Directory, typeof(object) /* Replace with actual type */ },
            { QueryResponseType.MajorBlock, typeof(object) /* Replace with actual type */ },
            { QueryResponseType.Network, typeof(Api.V2.NetworkStatusResponse) },
            { QueryResponseType.Partition, typeof(Api.V2.PartitionResponse) },
            { QueryResponseType.Version, typeof(Api.V2.VersionResponse) },
            { QueryResponseType.Receipt, typeof(object) /* Replace with actual type */ },
            { QueryResponseType.Error, typeof(object) /* Replace with actual type */ }
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
    }
} 