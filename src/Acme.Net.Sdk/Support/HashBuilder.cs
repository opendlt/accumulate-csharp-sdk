using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics; // For BigInteger
using System.Text;
using Acme.Net.Sdk.Protocol; // Ensure Protocol namespace is included

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Helper class to build a list of hashes and compute either a Merkle root or a checksum.
    /// </summary>
    public class HashBuilder
    {
        private readonly List<byte[]> _hashList = new List<byte[]>();

        /// <summary>
        /// Adds a pre-computed hash directly to the list.
        /// Null or empty arrays are ignored.
        /// </summary>
        /// <param name="value">The hash to add.</param>
        /// <returns>The current <see cref="HashBuilder"/> instance.</returns>
        public HashBuilder AddHash(byte[]? value)
        {
            if (value != null && value.Length > 0)
            {
                _hashList.Add(value);
            }
            return this;
        }

        /// <summary>
        /// Computes the SHA-256 hash of the given bytes and adds it to the list.
        /// Null or empty arrays are ignored.
        /// </summary>
        /// <param name="value">The bytes to hash and add.</param>
        /// <returns>The current <see cref="HashBuilder"/> instance.</returns>
        public HashBuilder AddBytes(byte[]? value)
        {
            if (value != null && value.Length > 0)
            {
                AddInternal(value);
            }
            return this;
        }

        /// <summary>
        /// Computes the SHA-256 hash of the given Url (encoded as UTF-8 string) and adds it to the list.
        /// Null Urls are ignored.
        /// </summary>
        /// <param name="value">The Url to hash and add.</param>
        /// <returns>The current <see cref="HashBuilder"/> instance.</returns>
        public HashBuilder AddUrl(Url? value) // Changed back from string to Url
        {
            if (value != null)
            {
                // Use the String() method from the ported Url class
                AddInternal(Encoding.UTF8.GetBytes(value.String()));
            }
            return this;
        }

        /// <summary>
        /// Converts the BigInteger to bytes using Marshaller.ToUIntBuffer, hashes the bytes, and adds the hash to the list.
        /// Null values are ignored.
        /// </summary>
        /// <param name="value">The BigInteger value.</param>
        /// <returns>The current <see cref="HashBuilder"/> instance.</returns>
        /// <exception cref="NotImplementedException">Thrown because Marshaller class is not yet ported.</exception>
        /// <remarks>TODO: Implement fully when Marshaller class is ported.</remarks>
        public HashBuilder AddUInt(BigInteger? value)
        {
            if (value.HasValue)
            {
                // Original code: add(Marshaller.toUIntBuffer(value));
                throw new NotImplementedException("Depends on the Marshaller class which is not yet ported.");
                // byte[] bytesToHash = Marshaller.ToUIntBuffer(value.Value); // Needs Marshaller.ToUIntBuffer
                // AddInternal(bytesToHash);
            }
            return this;
        }

        /// <summary>
        /// Converts the long to bytes using Marshaller.ToUIntBuffer, hashes the bytes, and adds the hash to the list.
        /// Null values are ignored.
        /// </summary>
        /// <param name="value">The long value.</param>
        /// <returns>The current <see cref="HashBuilder"/> instance.</returns>
        /// <exception cref="NotImplementedException">Thrown because Marshaller class is not yet ported.</exception>
        /// <remarks>TODO: Implement fully when Marshaller class is ported.</remarks>
        public HashBuilder AddLong(long? value)
        {
            if (value.HasValue)
            {
                 // Original code: add(Marshaller.toUIntBuffer(value));
                throw new NotImplementedException("Depends on the Marshaller class which is not yet ported.");
                // byte[] bytesToHash = Marshaller.ToUIntBuffer(value.Value); // Needs Marshaller.ToUIntBuffer
                // AddInternal(bytesToHash);
           }
            return this;
        }

        /// <summary>
        /// Private helper to hash the input bytes using SHA-256 and add the result to the list.
        /// </summary>
        /// <param name="value">Bytes to hash.</param>
        private void AddInternal(byte[] value)
        {
            // This method assumes value is non-null and has length > 0 based on callers
            _hashList.Add(HashUtils.Sha256(value));
        }

        /// <summary>
        /// Calculates the Merkle root of all the hashes added to the builder.
        /// </summary>
        /// <returns>The calculated Merkle root hash.</returns>
        public byte[] MerkleHash()
        {
            var merkleRootBuilder = new MerkleRootBuilder();
            foreach (var hash in _hashList)
            {
                 // Assuming AddToMerkleTree handles potential nulls if _hashList could contain them (it shouldn't here)
                merkleRootBuilder.AddToMerkleTree(hash);
            }
            return merkleRootBuilder.GetMerkleRoot();
        }

        /// <summary>
        /// Calculates a simple checksum by concatenating all hashes in the list and then computing the SHA-256 hash of the result.
        /// </summary>
        /// <returns>The checksum hash.</returns>
        public byte[] GetCheckSum()
        {
            using (var memoryStream = new MemoryStream())
            {
                foreach (var bytes in _hashList)
                {
                    memoryStream.Write(bytes, 0, bytes.Length);
                }
                return HashUtils.Sha256(memoryStream.ToArray());
            }
        }
    }
} 