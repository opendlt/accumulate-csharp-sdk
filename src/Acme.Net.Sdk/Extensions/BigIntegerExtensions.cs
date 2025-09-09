using System;
using System.Numerics;

namespace Acme.Net.Sdk.Extensions
{
    /// <summary>
    /// Extension methods for BigInteger to handle protocol-specific serialization.
    /// </summary>
    public static class BigIntegerExtensions
    {
        /// <summary>
        /// Converts a BigInteger to big-endian byte array, removing any unnecessary sign bytes.
        /// </summary>
        /// <param name="value">The BigInteger value to convert.</param>
        /// <returns>Big-endian byte array representation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative.</exception>
        public static byte[] ToBigEndianBytes(this BigInteger value)
        {
            if (value < BigInteger.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative");
            }

            // Get little-endian bytes from BigInteger
            var bytes = value.ToByteArray();
            
            // Check if there's an extra sign byte (for positive numbers)
            bool hasSignByte = bytes.Length > 1 && bytes[bytes.Length - 1] == 0;
            int length = hasSignByte ? bytes.Length - 1 : bytes.Length;
            
            // Create result array and reverse bytes (little-endian to big-endian)
            var result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = bytes[length - 1 - i];
            }
            
            return result;
        }

        /// <summary>
        /// Creates a BigInteger from big-endian bytes.
        /// </summary>
        /// <param name="bytes">Big-endian byte array.</param>
        /// <returns>The BigInteger value.</returns>
        public static BigInteger FromBigEndianBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return BigInteger.Zero;
            }

            // Convert from big-endian to little-endian
            var littleEndian = new byte[bytes.Length + 1]; // Extra byte for sign
            for (int i = 0; i < bytes.Length; i++)
            {
                littleEndian[i] = bytes[bytes.Length - 1 - i];
            }
            // Ensure positive by adding zero sign byte
            littleEndian[bytes.Length] = 0;
            
            return new BigInteger(littleEndian);
        }
    }
}