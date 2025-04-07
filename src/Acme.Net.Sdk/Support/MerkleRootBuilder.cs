using System;
using System.Collections.Generic;
using System.Linq;

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Incrementally builds a Merkle root hash.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.support.MerkleRootBuilder.
    /// </summary>
    public class MerkleRootBuilder
    {
        private long _count = 0;
        // List can contain nulls, representing empty slots in the pending calculation
        private readonly List<byte[]?> _pending = new List<byte[]?>(); 
        // Stores all added hashes for reference, though not strictly needed for root calculation
        private readonly List<byte[]> _hashList = new List<byte[]>(); 

        /// <summary>
        /// Adds a hash to the Merkle tree calculation.
        /// </summary>
        /// <param name="sourceHash">The hash to add. The array is copied internally.</param>
        /// <returns>The resulting intermediate hash after combining with pending hashes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if sourceHash is null.</exception>
        public byte[] AddToMerkleTree(byte[] sourceHash)
        {
            if (sourceHash == null) throw new ArgumentNullException(nameof(sourceHash));

            // Copy the hash to ensure the original array isn't modified if it came from elsewhere
            byte[] hash = (byte[])sourceHash.Clone(); 

            _hashList.Add(hash);
            _count++;
            PadPending();

            for (int i = 0; i < _pending.Count; i++)
            {
                byte[]? v = _pending[i];
                if (v == null)
                {
                    _pending[i] = hash;
                    return hash; // Return the current hash as it filled an empty slot
                }
                // Combine the pending hash (v) with the current hash
                hash = HashUtils.Sha256Combine(v, hash); // Assuming HashUtils.Combine hashes left then right
                _pending[i] = null; // Clear the slot after use
            }
            // If the loop completes, it means the new hash combined through all existing pending slots
            return hash;
        }

        /// <summary>
        /// Ensures the pending list has space by adding a null placeholder if the last slot is filled.
        /// </summary>
        private void PadPending()
        {
            if (!_pending.Any() || _pending[_pending.Count - 1] != null)
            {
                _pending.Add(null);
            }
        }

        /// <summary>
        /// Gets the current Merkle root based on the hashes added so far.
        /// </summary>
        /// <returns>The Merkle root hash, or an empty byte array if no hashes have been added.</returns>
        public byte[] GetMerkleRoot()
        {
            byte[] mdRoot = Array.Empty<byte>();
            if (_count == 0)
            {
                return mdRoot;
            }

            foreach (byte[]? pendingHash in _pending)
            {
                if (pendingHash == null) continue;

                if (mdRoot.Length == 0)
                {
                    // First non-null pending hash becomes the initial root
                    mdRoot = (byte[])pendingHash.Clone(); 
                }
                else
                {
                     // Combine the pending hash with the current root (order matches Java: pendingHash first)
                    mdRoot = HashUtils.Sha256Combine(pendingHash, mdRoot); 
                }
            }
            return mdRoot;
        }

         /// <summary>
        /// Gets the list of all hashes added to the builder.
        /// Note: This is primarily for reference; the root is calculated using the pending list.
        /// </summary>
        public IReadOnlyList<byte[]> HashList => _hashList.AsReadOnly();

        /// <summary>
        /// Gets the total count of hashes added.
        /// </summary>
        public long Count => _count;
    }
} 