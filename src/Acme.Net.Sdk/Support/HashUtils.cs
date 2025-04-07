using System.Security.Cryptography;

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Provides utility methods for cryptographic hashing.
    /// </summary>
    public static class HashUtils
    {
        /// <summary>
        /// Computes the SHA-256 hash of the specified byte array.
        /// </summary>
        /// <param name="data">The input data to hash.</param>
        /// <returns>The computed SHA-256 hash.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if data is null.</exception>
        public static byte[] Sha256(byte[] data)
        {
            // ArgumentNullException is automatically thrown by HashData if data is null.
            return SHA256.HashData(data);
        }

        /// <summary>
        /// Computes the SHA-512 hash of the specified byte array.
        /// </summary>
        /// <param name="data">The input data to hash.</param>
        /// <returns>The computed SHA-512 hash.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if data is null.</exception>
        public static byte[] Sha512(byte[] data)
        {
            // ArgumentNullException is automatically thrown by HashData if data is null.
            return SHA512.HashData(data);
        }

        /// <summary>
        /// Concatenates two byte arrays and computes the SHA-256 hash of the result.
        /// </summary>
        /// <param name="left">The first byte array.</param>
        /// <param name="right">The second byte array.</param>
        /// <returns>The SHA-256 hash of the combined arrays.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if left or right is null.</exception>
        public static byte[] Sha256Combine(byte[] left, byte[] right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            byte[] combined = new byte[left.Length + right.Length];
            System.Buffer.BlockCopy(left, 0, combined, 0, left.Length);
            System.Buffer.BlockCopy(right, 0, combined, left.Length, right.Length);
            
            return Sha256(combined);
        }
    }
} 