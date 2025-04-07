using System;
using System.Linq;
using System.Text;
using Acme.Net.Sdk.Support; // For HashUtils

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents an Accumulate Lite Address, which is a specific type of Accumulate URL
    /// with embedded hash and checksum information in the host/authority.
    /// Corresponds to io.accumulatenetwork.sdk.protocol.LiteAddress.
    /// </summary>
    public class LiteAddress : Url
    {
        private readonly byte[]? _byteValue; // Holds the core 20 bytes if valid

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteAddress"/> class from a URL string.
        /// Parses the URL and validates the embedded checksum according to Lite Address rules.
        /// </summary>
        /// <param name="urlString">The lite address URL string.</param>
        /// <exception cref="ArgumentNullException">Thrown if urlString is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the urlString is not a valid Accumulate URL format.</exception>
        public LiteAddress(string urlString) : base(urlString)
        {
            // The base constructor (Url(urlString)) already performed initial URL parsing 
            // and threw exceptions if the basic format was invalid or host was missing.
            // Now, perform LiteAddress specific validation.
            _byteValue = ValidateAndExtractByteValue();
        }
        
        // Private constructor used by LiteTokenAddress
        internal LiteAddress(Url url) : base(url.Uri) 
        {
            _byteValue = ValidateAndExtractByteValue();
        }

        // Internal method to perform validation and extraction
        private byte[]? ValidateAndExtractByteValue()
        {
             const int checksumByteLength = 4;
             const int checksumHexStringLength = checksumByteLength * 2;

            try
            {
                string host = this.HostName; // Property from base Url class
                byte[] hostBytes = Convert.FromHexString(host);

                if (hostBytes.Length <= checksumByteLength)
                {
                    // Hostname doesn't contain enough bytes for value + checksum
                    return null; 
                }
                
                int valueLength = hostBytes.Length - checksumByteLength;
                byte[] potentialByteValue = hostBytes[0..valueLength]; // First N-4 bytes
                byte[] checksumFromHost = hostBytes[valueLength..]; // Last 4 bytes

                string authority = this.Authority; // Property from base Url class
                if (authority.Length <= checksumHexStringLength)
                {
                    // Authority string isn't long enough to contain the checksum hex part
                     return null;
                }
                // Authority string excluding the last 8 hex characters (4 checksum bytes)
                string authorityForChecksum = authority[0..^checksumHexStringLength]; 

                // Calculate checksum hash
                byte[] checkSumInputBytes = Encoding.UTF8.GetBytes(authorityForChecksum);
                byte[] checkSumHash = HashUtils.Sha256(checkSumInputBytes);

                // Extract last 4 bytes from calculated checksum hash
                // Need to handle case where hash isn't 32 bytes, though SHA256 should be.
                if (checkSumHash.Length != 32)
                {
                    // Should not happen with SHA256
                    return null;
                }
                 byte[] calculatedChecksumBytes = checkSumHash[28..32]; 

                // Compare checksums
                if (checksumFromHost.SequenceEqual(calculatedChecksumBytes))
                {
                    // Checksum matches, store the value part
                    return potentialByteValue;
                }
                else
                {
                    // Checksum mismatch
                    return null;
                }
            }
            catch (FormatException) // Handle invalid hex in hostname
            {
                return null;
            }
            catch (Exception) // Catch potential errors from base properties if URL was malformed despite initial parse
            {
                // Log error? For now, treat as invalid.
                return null;
            }
        }

        /// <summary>
        /// Gets the core 20-byte value extracted from the Lite Address hostname if the checksum was valid.
        /// Returns null if the address format or checksum was invalid.
        /// </summary>
        /// <returns>The byte array value or null.</returns>
        public byte[]? GetByteValue() => _byteValue;

        /// <summary>
        /// Gets a value indicating whether the parsed Lite Address had a valid format and checksum.
        /// </summary>
        public bool IsValid => _byteValue != null;
    }
}
