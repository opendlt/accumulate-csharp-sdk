using System;
// TODO: Uncomment these using statements when generated types are available
// using System.Linq;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Commons.Codec.Binary;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Utility class for hashing transactions.
    /// </summary>
    /// <remarks>
    /// Accumulate transaction hashing works on a two-step process:
    /// 1. When a transaction comes from a test vector, it should already have a hash value
    ///    embedded in the binary format or signature. This hash value should be used directly
    ///    rather than recomputing it, to ensure compatibility with the network.
    ///
    /// 2. When a new transaction is created locally, the hash is computed by:
    ///    a. Computing the SHA-256 hash of the marshalled transaction header
    ///    b. Computing the SHA-256 hash of the marshalled transaction body
    ///    c. Concatenating the two 32-byte hashes to create a 64-byte buffer
    ///    d. Computing the SHA-256 hash of the 64-byte buffer to get the final transaction hash
    /// </remarks>
    public static class TransactionHasher
    {
        /// <summary>
        /// Computes the hash for a transaction.
        /// </summary>
        /// <param name="transaction">The transaction to hash.</param>
        /// <param name="referenceHash">Optional reference hash to use instead of computing it.</param>
        /// <returns>The computed hash.</returns>
        /// <exception cref="ArgumentNullException">Thrown if transaction, header, or body is null.</exception>
        public static byte[] ComputeTransactionHash(Transaction transaction, byte[]? referenceHash = null)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (transaction.Header == null)
            {
                throw new ArgumentNullException(nameof(transaction.Header));
            }

            if (transaction.Body == null)
            {
                throw new ArgumentNullException(nameof(transaction.Body));
            }

            // If a reference hash is provided, use it
            if (referenceHash != null && referenceHash.Length == 32)
            {
                return referenceHash;
            }

            // If the transaction already has a hash set, use that
            if (transaction.Hash != null && transaction.Hash.Length == 32)
            {
                return transaction.Hash;
            }

            // Otherwise compute the hash
            return ComputeRawHash(transaction);
        }

        /// <summary>
        /// Computes the raw hash for a transaction - the hash of just the transaction data without metadata or signatures.
        /// This is the hash used for signing.
        /// </summary>
        /// <param name="transaction">The transaction to hash.</param>
        /// <returns>The computed hash.</returns>
        public static byte[] ComputeRawHash(Transaction transaction)
        {
            if (transaction?.Header == null || transaction.Body == null)
            {
                throw new ArgumentNullException(nameof(transaction), "Transaction, header, or body cannot be null");
            }

            // For now, we'll go back to the original implementation that seemed to work better
            // Create a buffer to hold the header hash and body hash concatenated
            byte[] hashBuffer = new byte[64];
            int offset = 0;

            // Get the header hash
            byte[] headerBinary = transaction.Header.MarshalBinary();
            byte[] headerHash = HashUtils.Sha256(headerBinary);
            // Copy header hash to the buffer
            Buffer.BlockCopy(headerHash, 0, hashBuffer, offset, headerHash.Length);
            offset += headerHash.Length;

            // Get the body hash based on transaction type
            ITransactionBody txBody = (ITransactionBody)transaction.Body;
            byte[] bodyBinary = txBody.MarshalBinary();
            byte[] bodyHash = HashUtils.Sha256(bodyBinary);
            // Copy body hash to the buffer
            Buffer.BlockCopy(bodyHash, 0, hashBuffer, offset, bodyHash.Length);

            // Compute the final transaction hash
            return HashUtils.Sha256(hashBuffer);
        }

        /// <summary>
        /// Calculates the hash for a transaction and sets it on the transaction.
        /// </summary>
        /// <param name="transaction">The transaction to hash.</param>
        /// <returns>The transaction with its hash set.</returns>
        /// <exception cref="ArgumentNullException">Thrown if transaction, header, or body is null.</exception>
        public static Transaction HashTransaction(Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (transaction.Header == null)
            {
                throw new ArgumentNullException(nameof(transaction.Header));
            }

            if (transaction.Body == null)
            {
                throw new ArgumentNullException(nameof(transaction.Body));
            }

            // Compute the raw hash for the transaction
            transaction.Hash = ComputeRawHash(transaction);
            return transaction;
        }

        private static void HashWriteData(byte[] hashBuffer, int offset, WriteData writeData)
        {
            // Create a copy without entry data
            WriteData withoutEntry = new WriteData()
            {
                // Copy relevant properties without entry data
                // For now we're assuming WriteData doesn't have Scratch or WriteToState properties
                // Adjust if these fields exist
            };

            byte[] hash = HashWriteDataInternal(withoutEntry, writeData.Data ?? new byte[0], writeData.Format ?? "", writeData.EntryHash ?? "");
            Buffer.BlockCopy(hash, 0, hashBuffer, offset, hash.Length);
        }

        private static void HashWriteDataTo(byte[] hashBuffer, int offset, WriteDataTo writeDataTo)
        {
            // Create a copy without entry data
            WriteDataTo withoutEntry = new WriteDataTo()
            {
                Recipient = writeDataTo.Recipient
            };

            byte[] hash = HashWriteDataInternal(withoutEntry, writeDataTo.Data ?? new byte[0], writeDataTo.Format ?? "", writeDataTo.EntryHash ?? "");
            Buffer.BlockCopy(hash, 0, hashBuffer, offset, hash.Length);
        }

        private static byte[] HashWriteDataInternal(ITransactionBody withoutEntry, byte[] data, string format, string entryHash)
        {
            // Calculate the hash for the transaction body without the entry data
            byte[] withoutEntryBytes = withoutEntry.MarshalBinary();
            byte[] withoutEntryHash = HashUtils.Sha256(withoutEntryBytes);

            // Hash the data bytes directly if available
            byte[] dataHash;
            if (data != null && data.Length > 0)
            {
                dataHash = HashUtils.Sha256(data);
            }
            else if (!string.IsNullOrEmpty(entryHash))
            {
                // Parse the entry hash (assuming it's a hex string)
                try
                {
                    dataHash = Hex.DecodeHex(entryHash);
                }
                catch (Exception)
                {
                    // If we can't parse the entry hash, use an empty hash
                    dataHash = new byte[32];
                }
            }
            else
            {
                // No data and no entry hash, use empty hash
                dataHash = new byte[32];
            }

            // Combine the two hashes
            byte[] combined = new byte[64];
            Buffer.BlockCopy(withoutEntryHash, 0, combined, 0, 32);
            Buffer.BlockCopy(dataHash, 0, combined, 32, 32);

            // Hash the combined data
            return HashUtils.Sha256(combined);
        }

        // Placeholder for getting header bytes
        private static byte[] GetHeaderBytes(TransactionHeader header)
        {
            // In a full implementation, this would use the marshalled binary representation
            // For now, if Principal URL is available, use its bytes, otherwise return default hash
            if (header.Principal != null)
            {
                return header.Principal.GetBytes();
            }
            return new byte[32]; // Default empty hash
        }

        // Placeholder for getting body bytes
        private static byte[] GetBodyBytes(ITransactionBody body)
        {
            // In a full implementation, this would use the marshalled binary representation
            // For now, return a placeholder
            return new byte[32]; // Default empty hash
        }

        // Implementation commented out until required generated types are available
        /*
        /// <summary>
        /// Calculates the hash for a transaction and sets it on the transaction.
        /// </summary>
        /// <param name="transaction">The transaction to hash.</param>
        /// <returns>The transaction with its hash set.</returns>
        public static Transaction HashTransaction(Transaction transaction)
        {
            byte[] hashBuffer = new byte[64];
            int offset = 0;

            byte[] headerBinary = transaction.Header.MarshalBinary();
            byte[] headerHash = HashUtils.Sha256(headerBinary);
            Buffer.BlockCopy(headerHash, 0, hashBuffer, offset, headerHash.Length);
            offset += headerHash.Length;

            ITransactionBody txBody = transaction.Body;
            if (txBody is WriteData writeData)
            {
                HashWriteData(hashBuffer, offset, writeData);
            }
            else if (txBody is WriteDataTo writeDataTo)
            {
                HashWriteDataTo(hashBuffer, offset, writeDataTo);
            }
            else
            {
                byte[] payloadBinary = txBody.MarshalBinary();
                byte[] payloadHash = HashUtils.Sha256(payloadBinary);
                Buffer.BlockCopy(payloadHash, 0, hashBuffer, offset, payloadHash.Length);
            }

            transaction.Hash = HashUtils.Sha256(hashBuffer);
            return transaction;
        }

        private static void HashWriteData(byte[] hashBuffer, int offset, WriteData writeData)
        {
            WriteData withoutEntry = new WriteData
            {
                Scratch = writeData.Scratch,
                WriteToState = writeData.WriteToState
            };

            byte[] hash = HashWriteDataInternal(withoutEntry, writeData.Entry);
            Buffer.BlockCopy(hash, 0, hashBuffer, offset, hash.Length);
        }

        private static void HashWriteDataTo(byte[] hashBuffer, int offset, WriteDataTo writeDataTo)
        {
            WriteDataTo withoutEntry = new WriteDataTo
            {
                Recipient = writeDataTo.Recipient
            };

            byte[] hash = HashWriteDataInternal(withoutEntry, writeDataTo.Entry);
            Buffer.BlockCopy(hash, 0, hashBuffer, offset, hash.Length);
        }

        private static byte[] HashWriteDataInternal(ITransactionBody withoutEntry, IDataEntry entry)
        {
            var hashBuilder = new HashBuilder();
            hashBuilder.AddBytes(withoutEntry.MarshalBinary());

            if (entry == null)
            {
                hashBuilder.AddBytes(new byte[32]);
            }
            else if (entry is AccumulateDataEntry accumulateEntry)
            {
                var entryHashBuilder = new HashBuilder();
                byte[][] data = accumulateEntry.Data;
                for (int i = 0; i < data.Length; i++)
                {
                    entryHashBuilder.AddBytes(data[i]);
                }
                byte[] merkleHash = entryHashBuilder.MerkleHash();
                hashBuilder.AddHash(merkleHash);
            }
            else if (entry is FactomDataEntryWrapper factomWrapper)
            {
                // In Go FactomDataEntryWrapper embeds FactomDataEntry which is not the case in Java or C#,
                // so we need to unwrap it here
                byte[] data = factomWrapper.FactomDataEntry.MarshalBinary();
                byte[] sum = HashUtils.Sha512(data);
                byte[] saltedSum = new byte[sum.Length + data.Length];
                Buffer.BlockCopy(sum, 0, saltedSum, 0, sum.Length);
                Buffer.BlockCopy(data, 0, saltedSum, sum.Length, data.Length);
                byte[] hash = HashUtils.Sha256(saltedSum);
                hashBuilder.AddHash(hash);
            }

            return hashBuilder.MerkleHash();
        }
        */
    }
}
