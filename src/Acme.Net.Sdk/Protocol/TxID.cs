using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents an Accumulate Transaction ID (TxID).
    /// A TxID consists of an Accumulate URL and a transaction hash derived from that URL.
    /// Format typically expected: acc://{hash}@{account_url}
    /// Serializes/deserializes as the URL string in JSON.
    /// </summary>
    [JsonConverter(typeof(TxIDConverter))]
    public class TxID : IEquatable<TxID>
    {
        private Url _url;
        private byte[]? _hash; // Lazily extracted from URL

        /// <summary>
        /// Initializes a new instance of the <see cref="TxID"/> class.
        /// Required for deserialization and default construction. Url must be set later.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if Url is accessed before being set.</exception>
        private TxID() 
        {
            // Private constructor for JsonConverter, url MUST be set by converter or properties
             _url = null!; // Mark as non-null knowing it will be set, throw if accessed early
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TxID"/> class from a Url.
        /// </summary>
        /// <param name="url">The Accumulate URL representing the transaction.</param>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public TxID(Url url)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            // Hash is extracted on demand via GetHash()
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TxID"/> class from a URL string.
        /// </summary>
        /// <param name="urlString">The string representation of the Accumulate URL.</param>
        /// <exception cref="ArgumentNullException">Thrown if urlString is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the urlString is not a valid Accumulate URL format.</exception>
        public TxID(string urlString)
        {
            if (string.IsNullOrEmpty(urlString))
            {
                throw new ArgumentNullException(nameof(urlString));
            }
            // Use Url.Parse for validation
            _url = Url.Parse(urlString);
            // Hash is extracted on demand via GetHash()
        }

        /// <summary>
        /// Gets the Accumulate URL associated with this TxID.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the URL was not properly initialized (e.g., via default constructor without subsequent setting).</exception>
        public Url GetUrl()
        {
             if (_url == null) throw new InvalidOperationException("URL has not been initialized for this TxID.");
             return _url;
        }


        /// <summary>
        /// Gets the transaction hash extracted from the URL.
        /// The hash is extracted lazily upon first access.
        /// </summary>
        /// <returns>The transaction hash as a byte array.</returns>
        /// <exception cref="FormatException">Thrown if the URL does not contain a valid hex-encoded hash in the expected format (acc://{hash}@...).</exception>
        /// <exception cref="InvalidOperationException">Thrown if the URL was not properly initialized.</exception>
        public byte[] GetHash()
        {
            if (_hash == null)
            {
                ExtractHash(); // Throws if URL is null or format is wrong
            }
             // ExtractHash ensures _hash is not null if it returns successfully
            return _hash!;
        }

        /// <summary>
        /// Extracts the hash from the _url field.
        /// Expected format: acc://{hash}@{authority}/...
        /// </summary>
        private void ExtractHash()
        {
             if (_url == null) throw new InvalidOperationException("URL has not been initialized for this TxID.");

            string urlStr = _url.String(); // Get the canonical string representation
            int schemaEndIdx = urlStr.IndexOf("://");
            if (schemaEndIdx == -1)
            {
                throw new FormatException($"Invalid TxID URL format: Missing '://' in '{urlStr}'.");
            }
            int hashStartIdx = schemaEndIdx + 3; // Move past "://"

            int atIdx = urlStr.IndexOf('@', hashStartIdx);
            if (atIdx == -1)
            {
                throw new FormatException($"Invalid TxID URL format: Missing '@' after hash part in '{urlStr}'.");
            }

            string hashHex = urlStr.Substring(hashStartIdx, atIdx - hashStartIdx);
            if (string.IsNullOrEmpty(hashHex))
            {
                 throw new FormatException($"Invalid TxID URL format: Hash part is empty in '{urlStr}'.");
            }

            try
            {
                // .NET 5+ provides Convert.FromHexString
                _hash = Convert.FromHexString(hashHex);
                if (_hash.Length == 0) // Ensure hash isn't empty after conversion
                {
                     throw new FormatException($"Invalid TxID URL format: Decoded hash is empty in '{urlStr}'.");
                }
            }
            catch (FormatException fe)
            {
                throw new FormatException($"Invalid TxID URL format: Hash part '{hashHex}' is not valid hex in '{urlStr}'.", fe);
            }
        }

        /// <summary>
        /// Returns the string representation of the TxID (which is its URL string).
        /// </summary>
        /// <returns>The URL string.</returns>
        public override string ToString()
        {
            return _url?.String() ?? string.Empty; // Handle case where URL might not be set (e.g. during construction)
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as TxID);
        }

        /// <summary>
        /// Determines whether the specified TxID is equal to the current TxID.
        /// Comparison is based on the URL and the lazily-computed hash.
        /// </summary>
        /// <param name="other">The TxID to compare with the current TxID.</param>
        /// <returns>true if the specified TxID is equal to the current TxID; otherwise, false.</returns>
        public bool Equals(TxID? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare URLs first
            if (!object.Equals(this.GetUrl(), other.GetUrl())) return false;

            // Compare hashes (compute if necessary)
            // Use SequenceEqual for byte array comparison
            // Catch FormatException if hash extraction fails for either TxID during comparison
            try
            {
                return this.GetHash().SequenceEqual(other.GetHash());
            }
            catch (FormatException)
            {
                // If hashes cannot be extracted consistently, they are not equal in context
                return false;
            }
            catch (InvalidOperationException)
            {
                 // If URLs weren't initialized, they aren't equal
                 return false;
            }
        }

        /// <summary>
        /// Serves as the default hash function.
        /// Combines the hash codes of the URL and the lazily-computed hash.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
             // Use HashCode.Combine for modern approach
            HashCode hc = new HashCode();
            hc.Add(GetUrl()); // Add Url hash code
            
            // Add hash byte array hash code (compute if necessary)
            // Need to handle potential exceptions during GetHash()
            try
            {
                 byte[] h = GetHash();
                 // Add bytes element by element or use a helper if available for structural hashing
                 // For simplicity, add length and first/last element if exists
                 hc.Add(h.Length);
                 if(h.Length > 0) {
                     hc.Add(h[0]);
                     hc.Add(h[^1]); // Index from end operator (^1)
                 }
            }
            catch (FormatException) { /* If hash invalid, don't include in hashcode */ }
            catch (InvalidOperationException) { /* If URL invalid, don't include hash */ }
            
            return hc.ToHashCode();
        }

        public static bool operator ==(TxID? left, TxID? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

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