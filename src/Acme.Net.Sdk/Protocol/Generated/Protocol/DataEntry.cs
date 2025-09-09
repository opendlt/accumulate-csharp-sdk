using System;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Enum for DataEntry types matching the JavaScript SDK.
    /// </summary>
    public enum DataEntryType
    {
        Unknown = 0,
        Factom = 1,
        Accumulate = 2,
        DoubleHash = 3
    }

    /// <summary>
    /// Base interface for DataEntry union types.
    /// </summary>
    public interface IDataEntry : IMarshallable
    {
        DataEntryType EntryType { get; }
    }

    /// <summary>
    /// AccumulateDataEntry represents data stored in the Accumulate format.
    /// </summary>
    public class AccumulateDataEntry : IDataEntry
    {
        public DataEntryType EntryType => DataEntryType.Accumulate;
        
        /// <summary>
        /// Gets or sets the data segments.
        /// </summary>
        public byte[][]? Data { get; set; }

        public AccumulateDataEntry()
        {
        }

        public AccumulateDataEntry(byte[] singleData)
        {
            Data = new[] { singleData };
        }

        public AccumulateDataEntry(byte[][] data)
        {
            Data = data;
        }

        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Field 1: type
            marshaller.WriteUInt(1, (int)DataEntryType.Accumulate);
            
            // Field 2: data (repeatable)
            if (Data != null)
            {
                foreach (var segment in Data)
                {
                    if (segment != null)
                    {
                        marshaller.WriteBytes(2, segment);
                    }
                }
            }
            
            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// DoubleHashDataEntry represents data stored with double hashing.
    /// </summary>
    public class DoubleHashDataEntry : IDataEntry
    {
        public DataEntryType EntryType => DataEntryType.DoubleHash;
        
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public byte[][]? Data { get; set; }
        
        /// <summary>
        /// Gets or sets whether to include account ID in hash.
        /// </summary>
        public bool AccountID { get; set; }

        public DoubleHashDataEntry()
        {
        }

        public DoubleHashDataEntry(byte[] singleData)
        {
            Data = new[] { singleData };
        }

        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Field 1: type
            marshaller.WriteUInt(1, (int)DataEntryType.DoubleHash);
            
            // Field 2: data (repeatable)
            if (Data != null)
            {
                foreach (var segment in Data)
                {
                    if (segment != null)
                    {
                        marshaller.WriteBytes(2, segment);
                    }
                }
            }
            
            // Field 3: accountID
            if (AccountID)
            {
                marshaller.WriteBool(3, AccountID);
            }
            
            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// FactomDataEntryWrapper represents Factom data entry format.
    /// </summary>
    public class FactomDataEntryWrapper : IDataEntry
    {
        public DataEntryType EntryType => DataEntryType.Factom;
        
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public byte[]? Data { get; set; }
        
        /// <summary>
        /// Gets or sets the ExtIDs.
        /// </summary>
        public byte[][]? ExtIDs { get; set; }
        
        /// <summary>
        /// Gets or sets whether to include account ID in hash.
        /// </summary>
        public bool AccountID { get; set; }

        public FactomDataEntryWrapper()
        {
        }

        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Field 1: type
            marshaller.WriteUInt(1, (int)DataEntryType.Factom);
            
            // Field 2: data
            if (Data != null)
            {
                marshaller.WriteBytes(2, Data);
            }
            
            // Field 3: extIDs (repeatable)
            if (ExtIDs != null)
            {
                foreach (var extId in ExtIDs)
                {
                    if (extId != null)
                    {
                        marshaller.WriteBytes(3, extId);
                    }
                }
            }
            
            // Field 4: accountID
            if (AccountID)
            {
                marshaller.WriteBool(4, AccountID);
            }
            
            return marshaller.GetBytes();
        }
    }

    /// <summary>
    /// Helper class to create DataEntry from legacy WriteData fields.
    /// </summary>
    public static class DataEntryHelper
    {
        /// <summary>
        /// Creates an AccumulateDataEntry from legacy WriteData fields.
        /// </summary>
        public static AccumulateDataEntry FromLegacyFields(byte[]? data, string? format, string? entryHash)
        {
            var entry = new AccumulateDataEntry();
            var segments = new System.Collections.Generic.List<byte[]>();
            
            // Add data if present
            if (data != null)
            {
                segments.Add(data);
            }
            
            // Add format as UTF-8 bytes if present
            if (!string.IsNullOrEmpty(format))
            {
                segments.Add(System.Text.Encoding.UTF8.GetBytes(format));
            }
            
            // Add entry hash as hex bytes if present
            if (!string.IsNullOrEmpty(entryHash))
            {
                // Assuming entryHash is a hex string
                var hashBytes = new byte[entryHash.Length / 2];
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashBytes[i] = Convert.ToByte(entryHash.Substring(i * 2, 2), 16);
                }
                segments.Add(hashBytes);
            }
            
            if (segments.Count > 0)
            {
                entry.Data = segments.ToArray();
            }
            
            return entry;
        }
        
        /// <summary>
        /// Creates a DoubleHashDataEntry from data segments.
        /// This is what test vectors typically use.
        /// </summary>
        public static DoubleHashDataEntry CreateDoubleHashEntry(params byte[][] dataSegments)
        {
            return new DoubleHashDataEntry
            {
                Data = dataSegments
            };
        }
    }
}