using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Buffers.Binary;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Abstract base class for Factom data entries, providing common marshalling logic.
    /// Implements the IDataEntry interface.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.FactomDataEntryMarshaller.
    /// </summary>
    public abstract class FactomDataEntryMarshaller : IDataEntry
    {
        /// <summary>
        /// Gets the Account ID (Chain ID) associated with this entry.
        /// </summary>
        public abstract byte[] AccountId { get; }

        /// <summary>
        /// Gets the list of External IDs associated with this entry.
        /// </summary>
        public abstract IList<byte[]> ExtIds { get; }

        /// <summary>
        /// Gets the data payload of this entry.
        /// </summary>
        public abstract byte[]? Data { get; }

        /// <summary>
        /// Marshals the Factom data entry into its binary representation according to Factom's rules.
        /// Format: VersionByte (0x00) | AccountId (ChainId) | len(ExtIdsBuffer) (ushort BE) | ExtIdsBuffer | Data
        /// </summary>
        /// <returns>A byte array containing the marshalled data.</returns>
        /// <exception cref="InvalidOperationException">Thrown if AccountId is null or ExtIds collection is null.</exception>
        /// <exception cref="IOException">Thrown if writing to the memory stream fails.</exception>
        public byte[] MarshalBinary()
        {
            if (AccountId == null) throw new InvalidOperationException("AccountId cannot be null.");
            if (ExtIds == null) throw new InvalidOperationException("ExtIds cannot be null.");

            using var memoryStream = new MemoryStream();
            // BinaryWriter uses LittleEndian by default, so we write primitive types like ushort manually for BigEndian.
            using (var writer = new BinaryWriter(memoryStream)) 
            {
                // Header, version byte 0x00
                writer.Write((byte)0x00); 
                
                // Account ID (Chain ID)
                writer.Write(AccountId);
                
                // Marshal External IDs
                byte[] extIdsBuffer = MarshalExtIds();
                
                // Write Length of ExtIdsBuffer (ushort BigEndian)
                if (extIdsBuffer.Length > ushort.MaxValue) {
                     throw new InvalidOperationException("Marshalled External IDs exceed maximum representable length (ushort).");
                }
                ushort extIdsLength = (ushort)extIdsBuffer.Length;
                // Manual BigEndian write for ushort
                writer.Write((byte)(extIdsLength >> 8));   // High byte
                writer.Write((byte)(extIdsLength & 0xFF)); // Low byte
                
                // Write ExtIdsBuffer
                writer.Write(extIdsBuffer);
                
                // Write Data payload (if present)
                byte[]? data = Data;
                if (data != null)
                {
                    writer.Write(data);
                }
            }
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Marshals the list of External IDs.
        /// Format: For each ExtId: len(ExtId) (ushort BE) | ExtId
        /// </summary>
        /// <returns>A byte array containing the marshalled external IDs.</returns>
        private byte[] MarshalExtIds()
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new BinaryWriter(memoryStream)) 
            {
                foreach (byte[] extId in ExtIds) 
                {
                    if (extId == null) continue; // Or throw? Java version likely didn't allow nulls in the list.
                    
                    if (extId.Length > ushort.MaxValue) {
                         throw new InvalidOperationException("External ID length exceeds maximum representable length (ushort).");
                    }
                    ushort extIdLength = (ushort)extId.Length;
                     // Manual BigEndian write for ushort
                    writer.Write((byte)(extIdLength >> 8));   // High byte
                    writer.Write((byte)(extIdLength & 0xFF)); // Low byte
                    
                    // Write ExtId bytes
                    writer.Write(extId);
                }
            }
            return memoryStream.ToArray();
        }
    }
}

