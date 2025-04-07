using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters; // For StringEnumConverter
using Newtonsoft.Json.Linq;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents the type of network partition (Directory or Block Validator).
    /// Implements IIntValueEnum for marshalling and uses custom JSON conversion.
    /// Corresponds to the Java enum io.accumulatenetwork.sdk.protocol.NetworkType.
    /// </summary>
    [JsonConverter(typeof(NetworkTypeConverter))]
    public readonly struct NetworkType : IEquatable<NetworkType>, IIntValueEnum
    {
        // Static instances mirroring the Java enum constants
        public static readonly NetworkType Directory = new NetworkType(1, "directory");
        public static readonly NetworkType BlockValidator = new NetworkType(2, "blockValidator");

        // Internal storage
        private readonly int _value;
        private readonly string _apiName;

        // Private constructor to control instantiation
        private NetworkType(int value, string apiName)
        {
            _value = value;
            _apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        }

        /// <summary>
        /// Gets the integer value associated with this network type.
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// Gets the API name string associated with this network type (used for JSON).
        /// </summary>
        public string ApiName => _apiName;

        // --- Static Factory Methods --- 
        
        private static readonly List<NetworkType> _allTypes = new List<NetworkType> { Directory, BlockValidator };

        /// <summary>
        /// Gets the NetworkType corresponding to the given integer value.
        /// </summary>
        /// <param name="value">The integer value (1 for Directory, 2 for BlockValidator).</param>
        /// <returns>The corresponding NetworkType.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value does not match a known NetworkType.</exception>
        public static NetworkType FromValue(int value)
        {
            // Corrected logic: Compare against _value, not ordinal
            foreach (var type in _allTypes)
            {
                if (type._value == value)
                {
                    return type;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(value), $"{value} is not a valid NetworkType value.");
        }

        /// <summary>
        /// Gets the NetworkType corresponding to the given API name string (case-insensitive).
        /// </summary>
        /// <param name="name">The API name string ("directory" or "blockValidator").</param>
        /// <returns>The corresponding NetworkType.</returns>
        /// <exception cref="ArgumentException">Thrown if the name is null, empty, or does not match a known NetworkType.</exception>
        public static NetworkType FromApiName(string? name)
        {
             if (string.IsNullOrEmpty(name))
             {
                  throw new ArgumentException("API name cannot be null or empty.", nameof(name));
             }
            foreach (var type in _allTypes)
            {
                if (name.Equals(type._apiName, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }
            throw new ArgumentException($"'{name}' is not a valid NetworkType API name.", nameof(name));
        }

        // --- Equality & Overrides ---

        public bool Equals(NetworkType other)
        {
            return _value == other._value && _apiName == other._apiName;
        }

        public override bool Equals(object? obj)
        {
            return obj is NetworkType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, _apiName);
        }

        public override string ToString()
        {
            return _apiName; // Matches Java toString() and @JsonValue behavior
        }

        public static bool operator ==(NetworkType left, NetworkType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NetworkType left, NetworkType right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Custom JsonConverter for NetworkType.
    /// Reads either the integer value or the API name string, writes the API name string.
    /// </summary>
    public class NetworkTypeConverter : JsonConverter<NetworkType>
    {
        public override void WriteJson(JsonWriter writer, NetworkType value, JsonSerializer serializer)
        {
            // Write the API name string
            writer.WriteValue(value.ApiName);
        }

        public override NetworkType ReadJson(JsonReader reader, Type objectType, NetworkType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            if (token.Type == JTokenType.String)
            {
                string? apiName = token.Value<string>();
                try
                {
                    return NetworkType.FromApiName(apiName);
                }
                catch (ArgumentException ex)
                {
                    throw new JsonSerializationException($"Error deserializing NetworkType: {ex.Message}", ex);
                }
            }
            else if (token.Type == JTokenType.Integer)
            {
                int value = token.Value<int>();
                try
                {
                    // Java version used ordinal, but seems incorrect. Use FromValue which uses the defined int value.
                    return NetworkType.FromValue(value);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                     throw new JsonSerializationException($"Error deserializing NetworkType: {ex.Message}", ex);
                }
            }

            throw new JsonSerializationException($"Unexpected token type for NetworkType: {token.Type}. Expected String or Integer.");
        }
    }

    // NOTE: This extension class might be unnecessary now that NetworkType is a struct
    // implementing IIntValueEnum directly. Keeping for parity/reference if needed elsewhere.
    public static class NetworkTypeExtensions
    {
        /// <summary>
        /// Gets the integer value associated with the NetworkType.
        /// </summary>
        /// <param name="type">The network type.</param>
        /// <returns>The associated integer value (long).</returns>
        public static int GetValue(this NetworkType type) // Changed return type to int to match Value property
        {
            // FIX: Return the Value property from the struct
            return type.Value;
        }

        /// <summary>
        /// Gets the API name (string representation) associated with the NetworkType.
        /// Uses the EnumMember attribute value.
        /// </summary>
        /// <param name="type">The network type.</param>
        /// <returns>The API name string.</returns>
        public static string GetApiName(this NetworkType type)
        {
            var enumType = typeof(NetworkType);
            var memberInfo = enumType.GetMember(type.ToString());
            var enumMemberAttribute = memberInfo.FirstOrDefault()?.
                GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .OfType<EnumMemberAttribute>()
                .FirstOrDefault();
            
            return enumMemberAttribute?.Value ?? type.ToString().ToLowerInvariant(); // Fallback logic
        }

        // Note: The custom `from*` static methods from Java are generally handled 
        // by Newtonsoft.Json's StringEnumConverter or direct enum parsing/casting in C#.
        // Re-implementing them exactly might require a more complex custom JsonConverter 
        // if the lenient parsing (string or number) is strictly required.
    }
} 