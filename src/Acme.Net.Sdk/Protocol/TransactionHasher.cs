using System;
// TODO: Uncomment these using statements when generated types are available
// using System.Linq;
// using Acme.Net.Sdk.Protocol.Generated.Protocol;
// using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Utility class for hashing transactions.
    /// </summary>
    public static class TransactionHasher
    {
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
