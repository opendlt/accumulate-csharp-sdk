using System;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents an Accumulate Lite Token Address, which is a specific type of Lite Address 
    /// where the embedded value must be exactly 20 bytes long.
    /// Corresponds to io.accumulatenetwork.sdk.protocol.LiteTokenAddress.
    /// </summary>
    public class LiteTokenAddress : LiteAddress
    {
        private const int RequiredByteValueLength = 20;
        private readonly bool _isTokenAddressValid;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteTokenAddress"/> class from a URL string.
        /// Parses the URL, validates the embedded checksum via the base LiteAddress class, 
        /// and ensures the extracted value is exactly 20 bytes long.
        /// </summary>
        /// <param name="urlString">The lite token address URL string.</param>
        /// <exception cref="ArgumentNullException">Thrown if urlString is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the urlString is not a valid Accumulate URL format.</exception>
        public LiteTokenAddress(string urlString) : base(urlString)
        {
            // Base constructor performed initial parsing and checksum validation.
            // Now check if it was valid *and* if the byte value has the required length.
            _isTokenAddressValid = base.IsValid && base.GetByteValue()!.Length == RequiredByteValueLength;
        }
        
        // Internal constructor likely not needed unless specifically required elsewhere.
        // internal LiteTokenAddress(Url url) : base(url) {
        //     _isTokenAddressValid = base.IsValid && base.GetByteValue()!.Length == RequiredByteValueLength;
        // }

        /// <summary>
        /// Gets the 20-byte value extracted from the Lite Token Address hostname if the format and checksum were valid 
        /// and the value length was correct.
        /// Returns null otherwise.
        /// </summary>
        /// <returns>The 20-byte array value or null.</returns>
        public new byte[]? GetByteValue() => _isTokenAddressValid ? base.GetByteValue() : null;

        /// <summary>
        /// Gets a value indicating whether the parsed Lite Token Address had a valid format, checksum, 
        /// and the required 20-byte value length.
        /// </summary>
        public new bool IsValid => _isTokenAddressValid;
    }
}
