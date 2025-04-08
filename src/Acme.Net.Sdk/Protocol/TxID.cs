using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a transaction ID in the Accumulate protocol.
    /// </summary>
    [JsonConverter(typeof(TxIDConverter))]
    public class TxID
    {
        private readonly Url _url;
        private byte[]? _cachedHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="TxID"/> class.
        /// </summary>
        /// <param name="url">The URL representing the transaction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public TxID(Url url)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TxID"/> class.
        /// </summary>
        /// <param name="urlString">The URL string representing the transaction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if urlString is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the URL string is not in a valid format.</exception>
        public TxID(string urlString)
        {
            if (string.IsNullOrEmpty(urlString))
                throw new ArgumentNullException(nameof(urlString));
                
            _url = Url.Parse(urlString);
        }

        /// <summary>
        /// Gets the URL representation of the transaction ID.
        /// </summary>
        /// <returns>The URL representing the transaction ID.</returns>
        public Url GetUrl()
        {
            return _url;
        }

        /// <summary>
        /// Extracts the hash component from the transaction ID URL.
        /// </summary>
        /// <returns>The hash as a byte array.</returns>
        /// <exception cref="FormatException">Thrown if the URL does not contain a valid hash.</exception>
        public byte[] GetHash()
        {
            if (_cachedHash != null)
                return _cachedHash;

            string urlString = _url.String();
            int indexOfAt = urlString.IndexOf('@');
            
            if (indexOfAt <= 0 || !urlString.StartsWith("acc://"))
                throw new FormatException($"Invalid TxID URL format: {urlString}. Expected format is acc://[hash]@[authority][path]");

            string hashPart = urlString.Substring(6, indexOfAt - 6); // Skip "acc://" prefix
            
            if (string.IsNullOrEmpty(hashPart))
                throw new FormatException($"TxID URL does not contain a hash part: {urlString}");
                
            try
            {
                _cachedHash = Convert.FromHexString(hashPart);
                return _cachedHash;
            }
            catch (FormatException ex)
            {
                throw new FormatException($"Invalid hash format in TxID URL: {hashPart}", ex);
            }
        }

        /// <summary>
        /// Returns a string representation of the transaction ID.
        /// </summary>
        /// <returns>The string representation of the transaction ID.</returns>
        public override string ToString()
        {
            return _url.ToString();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is TxID other)
            {
                // Implement case-insensitive URL string comparison
                return string.Equals(_url.String(), other._url.String(), StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            // Use case-insensitive hash code for the URL string
            return _url.String().ToLowerInvariant().GetHashCode();
        }

        /// <summary>
        /// Compares two TxID objects for equality.
        /// </summary>
        /// <param name="left">The first TxID to compare.</param>
        /// <param name="right">The second TxID to compare.</param>
        /// <returns>true if the objects are equal; otherwise, false.</returns>
        public static bool operator ==(TxID? left, TxID? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            
            if (left is null || right is null)
                return false;
                
            return left.Equals(right);
        }
        
        /// <summary>
        /// Compares two TxID objects for inequality.
        /// </summary>
        /// <param name="left">The first TxID to compare.</param>
        /// <param name="right">The second TxID to compare.</param>
        /// <returns>true if the objects are not equal; otherwise, false.</returns>
        public static bool operator !=(TxID? left, TxID? right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Custom JsonConverter for TxID serialization/deserialization as a URL string.
    /// </summary>
    public class TxIDConverter : JsonConverter<TxID>
    {
        public override void WriteJson(JsonWriter writer, TxID? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                 try
                {
                    // Write the string representation of the URL
                    writer.WriteValue(value.GetUrl().String());
                }
                 catch (InvalidOperationException ex)
                 {
                     // Should not happen if TxID is properly constructed before serialization
                     throw new JsonSerializationException("Cannot serialize TxID: URL is not initialized.", ex);
                 }
            }
        }

        public override TxID? ReadJson(JsonReader reader, Type objectType, TxID? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                string? urlString = reader.Value as string;
                if (string.IsNullOrEmpty(urlString))
                {
                    // Represent null or empty string input as null TxID? Or throw?
                    // Java version likely threw implicitly via Url.toAccURL. Let's throw.
                     throw new JsonSerializationException("Cannot deserialize TxID from null or empty string.");
                }

                try
                {
                    // Use the TxID constructor that takes a string, which handles parsing and basic validation.
                    // The constructor uses Url.Parse internally.
                    var txid = new TxID(urlString);
                    
                    // Optionally, trigger hash extraction here to validate format during deserialization
                    // txid.GetHash(); // This would throw FormatException if hash is invalid

                    return txid;
                }
                // Catch exceptions from Url.Parse or TxID constructor
                catch (ArgumentNullException ex) // Should be caught by null/empty check above, but for safety
                {
                     throw new JsonSerializationException($"Error deserializing TxID: Input string is null or empty.", ex);
                }
                 catch (UriFormatException ex)
                {
                    throw new JsonSerializationException($"Error deserializing TxID: Invalid URL format '{urlString}'.", ex);
                }
                // Optionally catch FormatException if GetHash() is called above
                // catch (FormatException ex)
                // {
                //     throw new JsonSerializationException($"Error deserializing TxID: URL '{urlString}' does not contain a valid hash.", ex);
                // }
                 catch (Exception ex) // Catch any other unexpected exceptions during construction
                {
                    throw new JsonSerializationException($"Unexpected error deserializing TxID from string '{urlString}'.", ex);
                }
            }

            throw new JsonSerializationException($"Unexpected token type for TxID: {reader.TokenType}. Expected String or Null.");
        }
    }
} 