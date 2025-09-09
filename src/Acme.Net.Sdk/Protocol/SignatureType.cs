namespace Acme.Net.Sdk.Protocol.Generated // Mimicking Java package structure
{
    /// <summary>
    /// Placeholder for the generated SignatureType enum.
    /// Defines different types of Accumulate signatures.
    /// Values need to be confirmed/updated when actual generated code is available.
    /// </summary>
    public enum SignatureType
    {
        UNKNOWN = 0, // Default/Unknown
        ED25519 = 1,
        RCD1 = 2, 
        RECEIPT = 3,
        PARTITION = 4,
        INTERNAL = 5,
        AUTHORITY = 6,
        // Add other types as discovered/needed
    }
    
    /// <summary>
    /// Extension methods for SignatureType enum.
    /// </summary>
    public static class SignatureTypeExtensions
    {
        /// <summary>
        /// Converts a byte value to a SignatureType.
        /// </summary>
        /// <param name="value">The byte value to convert.</param>
        /// <returns>The corresponding SignatureType, or UNKNOWN if the value is not recognized.</returns>
        public static SignatureType FromValue(byte value)
        {
            if (Enum.IsDefined(typeof(SignatureType), (int)value))
            {
                return (SignatureType)value;
            }
            // Return UNKNOWN for unrecognized values
            return SignatureType.UNKNOWN;
        }
    }
}
