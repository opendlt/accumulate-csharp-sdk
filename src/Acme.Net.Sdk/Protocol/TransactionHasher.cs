using System;
// TODO: Uncomment these using statements when generated types are available
// using System.Linq;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Utility class for hashing transactions.
    /// </summary>
    public static class TransactionHasher
    {
        /// <summary>
        /// Computes the hash for a transaction.
        /// </summary>
        /// <param name="transaction">The transaction to hash.</param>
        /// <returns>The computed hash.</returns>
        /// <exception cref="ArgumentNullException">Thrown if transaction, header, or body is null.</exception>
        public static byte[] ComputeTransactionHash(Transaction transaction)
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

            // Calculate the hash by combining the header and body hashes
            var result = HashTransaction(transaction);
            return result.Hash ?? throw new InvalidOperationException("Transaction hash was not set properly");
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

            byte[] hashBuffer = new byte[64];
            int offset = 0;

            // Get the header hash
            byte[] headerBinary = transaction.Header.MarshalBinary();
            byte[] headerHash = HashUtils.Sha256(headerBinary);
            Buffer.BlockCopy(headerHash, 0, hashBuffer, offset, headerHash.Length);
            offset += headerHash.Length;

            // Get the body hash based on transaction type
            ITransactionBody txBody = (ITransactionBody)transaction.Body;
            
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

            // Compute the final transaction hash
            transaction.Hash = HashUtils.Sha256(hashBuffer);
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
                    dataHash = Commons.Codec.Binary.Hex.DecodeHex(entryHash);
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
