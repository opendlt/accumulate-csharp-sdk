using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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
            
            // Special handling for WriteData and WriteDataTo transactions
            if (txBody is Protocol.Generated.Protocol.WriteData writeData)
            {
                byte[] bodyHash = HashWriteData(writeData);
                Buffer.BlockCopy(bodyHash, 0, hashBuffer, offset, bodyHash.Length);
            }
            else if (txBody is Protocol.Generated.Protocol.WriteDataTo writeDataTo)
            {
                byte[] bodyHash = HashWriteDataTo(writeDataTo);
                Buffer.BlockCopy(bodyHash, 0, hashBuffer, offset, bodyHash.Length);
            }
            else
            {
                // Standard transaction hashing
                byte[] bodyBinary = txBody.MarshalBinary();
                byte[] bodyHash = HashUtils.Sha256(bodyBinary);
                Buffer.BlockCopy(bodyHash, 0, hashBuffer, offset, bodyHash.Length);
            }

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

        private static byte[] HashWriteData(WriteData writeData)
        {
            // Create a copy without the entry field
            var withoutEntry = new WriteData();
            
            // Marshal the body without the entry
            byte[] bodyWithoutEntry = withoutEntry.MarshalBinary();
            byte[] bodyHash = HashUtils.Sha256(bodyWithoutEntry);
            
            // Hash the entry data 
            byte[] entryHash = HashDataEntry(writeData.DataEntry);
            
            // Combine using DAG-style merkle hash (matches Go implementation)
            return ComputeMerkleHash(new[] { bodyHash, entryHash });
        }

        private static byte[] HashWriteDataTo(WriteDataTo writeDataTo)
        {
            // Create a copy without the entry field  
            var withoutEntry = new WriteDataTo();
            if (writeDataTo.Recipient != null)
            {
                withoutEntry.WithRecipient(writeDataTo.Recipient);
            }
            
            // Marshal the body without the entry
            byte[] bodyWithoutEntry = withoutEntry.MarshalBinary();
            byte[] bodyHash = HashUtils.Sha256(bodyWithoutEntry);
            
            // Hash the entry data
            byte[] entryHash = HashDataEntry(writeDataTo.DataEntry);
            
            // Combine using DAG-style merkle hash (matches Go implementation)
            return ComputeMerkleHash(new[] { bodyHash, entryHash });
        }
        
        private static byte[] HashDataEntry(Protocol.Generated.Protocol.IDataEntry? entry) => entry switch
        {
            null => new byte[ProtocolConstants.HashSizeBytes],
            AccumulateDataEntry acc => ComputeAccumulateDataHash(acc),
            DoubleHashDataEntry dbl => ComputeDoubleHashDataHash(dbl),
            FactomDataEntryWrapper fct => ComputeFactomDataHash(fct),
            _ => HashUtils.Sha256(entry.MarshalBinary())
        };
        
        private static byte[] ComputeAccumulateDataHash(AccumulateDataEntry entry)
        {
            if (entry.Data == null || entry.Data.Length == 0)
            {
                return new byte[ProtocolConstants.HashSizeBytes];
            }
            
            var hashes = entry.Data.Select(d => HashUtils.Sha256(d ?? Array.Empty<byte>())).ToArray();
            return ComputeMerkleHash(hashes);
        }
        
        private static byte[] ComputeDoubleHashDataHash(DoubleHashDataEntry entry)
        {
            if (entry.Data == null || entry.Data.Length == 0)
            {
                return new byte[ProtocolConstants.HashSizeBytes];
            }
            
            var hashes = entry.Data.Select(d => HashUtils.Sha256(d ?? Array.Empty<byte>())).ToArray();
            var merkleRoot = ComputeMerkleHash(hashes);
            // Double hash the merkle root (this is what makes it "DoubleHash")
            return HashUtils.Sha256(merkleRoot);
        }
        
        private static byte[] ComputeFactomDataHash(FactomDataEntryWrapper entry)
        {
            // For Factom, use special hashing (SHA512 then SHA256 with salt)
            byte[] data = entry.MarshalBinary();
            byte[] sum = HashUtils.Sha512(data);
            byte[] saltedSum = new byte[sum.Length + data.Length];
            Buffer.BlockCopy(sum, 0, saltedSum, 0, sum.Length);
            Buffer.BlockCopy(data, 0, saltedSum, sum.Length, data.Length);
            return HashUtils.Sha256(saltedSum);
        }
        
        private static byte[] ComputeMerkleHash(byte[][] hashes)
        {
            if (hashes == null || hashes.Length == 0)
            {
                return new byte[ProtocolConstants.HashSizeBytes];
            }
            
            if (hashes.Length == 1)
            {
                return hashes[0];
            }
            
            // Accumulate uses a DAG-style merkle algorithm, not a binary tree
            // Start with the first hash as anchor
            byte[] anchor = hashes[0];
            
            // Use ArrayPool to reduce allocations
            var pool = ArrayPool<byte>.Shared;
            
            // Chain subsequent hashes: anchor = SHA256(anchor || next)
            for (int i = 1; i < hashes.Length; i++)
            {
                if (hashes[i] != null)
                {
                    int combinedLength = anchor.Length + hashes[i].Length;
                    byte[] combined = pool.Rent(combinedLength);
                    
                    try
                    {
                        Buffer.BlockCopy(anchor, 0, combined, 0, anchor.Length);
                        Buffer.BlockCopy(hashes[i], 0, combined, anchor.Length, hashes[i].Length);
                        
                        // Create a span for the exact size we need
                        var dataSpan = new Span<byte>(combined, 0, combinedLength);
                        anchor = HashUtils.Sha256(dataSpan.ToArray());
                    }
                    finally
                    {
                        pool.Return(combined, clearArray: true);
                    }
                }
            }
            
            return anchor;
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
