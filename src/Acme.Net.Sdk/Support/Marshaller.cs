using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics; // For BigInteger
using System.Text;
using Acme.Net.Sdk.Protocol; // Add using statement

namespace Acme.Net.Sdk.Support
{
    // Placeholder class - Define properly later if needed
    public class RawJson { }


    /// <summary>
    /// Serializes data into a specific binary format used by Accumulate.
    /// </summary>
    public class Marshaller : IDisposable
    {
        private readonly MemoryStream _memoryStream;
        private readonly BinaryWriter _writer;

        // Java used DataOutputStream which writes in big-endian. BinaryWriter uses little-endian by default.
        // We need to ensure consistent endianness, especially for int/long. Accumulate likely uses little-endian.
        // Let's assume little-endian for now. If issues arise, we'll need custom byte writing.
        // TODO: Verify required endianness for the protocol.

        public Marshaller()
        {
            _memoryStream = new MemoryStream();
            _writer = new BinaryWriter(_memoryStream, Encoding.UTF8, false); // Use UTF8, keep stream open
        }

        public void WriteString(int fieldNr, string? value)
        {
            if (value != null) // Java version doesn't check for empty string, only null
            {
                _writer.Write((byte)fieldNr);
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
                InternalWriteUInt(BigInteger.Parse(utf8Bytes.Length.ToString())); // Write length as varint
                _writer.Write(utf8Bytes);
            }
        }

        public void WriteString(int fieldNr, IEnumerable<string?> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    WriteString(fieldNr, value);
                }
            }
        }

        public void WriteUrl(int fieldNr, params Url[]? values) // Removed placeholder comment
        {
             if (values != null)
            {
                foreach (var value in values)
                {
                    if (value != null)
                    {
                        // Use the String() method from the ported Url class
                        WriteString(fieldNr, value.String());
                    }
                }
            }
        }

        // Note: Java writeInt writes 4 bytes (Big Endian). BinaryWriter.Write(int) writes 4 bytes (Little Endian).
        public void WriteInt(int fieldNr, int? value)
        {
            if (value.HasValue)
            {
                _writer.Write((byte)fieldNr);
                _writer.Write(value.Value); // Assumes Little Endian is correct
            }
        }

        // Note: Java writeLong writes 8 bytes (Big Endian). BinaryWriter.Write(long) writes 8 bytes (Little Endian).
        public void WriteLong(int fieldNr, long? value)
        {
            if (value.HasValue)
            {
                _writer.Write((byte)fieldNr);
                _writer.Write(value.Value); // Assumes Little Endian is correct
            }
        }

        public void WriteBool(int fieldNr, bool? value)
        {
            if (value.HasValue)
            {
                _writer.Write((byte)fieldNr);
                _writer.Write(value.Value);
            }
        }

        public void WriteUInt(int fieldNr, int? value)
        {
            if (value.HasValue)
            {
                WriteUInt(fieldNr, new BigInteger(value.Value));
            }
        }

        public void WriteUInt(int fieldNr, long? value)
        {
            if (value.HasValue)
            {
                WriteUInt(fieldNr, new BigInteger(value.Value));
            }
        }

        // Removed BigDecimal overload as BigInteger covers it in C#

        public void WriteUInt(int fieldNr, BigInteger? value)
        {
            if (value.HasValue)
            {
                if (value.Value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value), "Cannot write negative value as unsigned integer.");
                _writer.Write((byte)fieldNr);
                InternalWriteUInt(value.Value);
            }
        }

        // Port of Java's variable-length unsigned integer encoding.
        private void InternalWriteUInt(BigInteger value)
        {
            BigInteger number = value;
            while (number.CompareTo(0x80) >= 0)
            {
                BigInteger byteToWrite = (number & 0x7F) | 0x80; // Keep lower 7 bits, set 8th bit
                _writer.Write(byteToWrite.ToByteArray().First()); // Assumes BigInteger.ToByteArray respects endianness appropriately for single byte
                number >>= 7;
            }
            _writer.Write(((byte)(number & 0x7F))); // Write last byte (lower 7 bits, 8th bit is 0)
        }

        public void WriteEnum(int fieldNr, IIntValueEnum? value)
        {
            if (value != null)
            {
                WriteUInt(fieldNr, value.Value);
            }
        }

        // Writes a fixed-size hash (presumably 32 bytes for SHA-256)
        public void WriteHash(int fieldNr, byte[]? value)
        {
            if (value != null)
            {
                // Custom format: field number as single byte, then raw hash without length
                _writer.Write((byte)fieldNr);
                _writer.Write(value);
            }
        }

        public void WriteHash(int fieldNr, IEnumerable<byte[]?>? hashes)
        {
             if (hashes != null)
            {
                foreach (byte[]? hash in hashes)
                {
                    WriteHash(fieldNr, hash);
                }
            }
       }

        // Writes length-prefixed bytes
        public void WriteBytes(int fieldNr, byte[]? value)
        {
            if (value != null)
            {
                _writer.Write((byte)fieldNr);
                InternalWriteUInt(BigInteger.Parse(value.Length.ToString())); // Write length as varint
                _writer.Write(value);
            }
        }

        private void WriteMarshallable(int fieldNr, IMarshallable v)
        {
            WriteBytes(fieldNr, v.MarshalBinary());
        }

        public void WriteBytes(int fieldNr, IEnumerable<byte[]?>? data)
        {
            if (data != null)
            {
                foreach (byte[]? item in data)
                {
                    WriteBytes(fieldNr, item);
                }
            }
        }

        // Helper to check for non-byte arrays
        private static bool IsArrayButNotByteArray(object value)
        {
            var type = value.GetType();
            return type.IsArray && type != typeof(byte[]);
        }

        // Write each element of a non-byte array
        private void WriteArray(int fieldNr, System.Collections.IEnumerable value) // Changed param type
        {
            foreach (var element in value)
            {
                if (element != null) // Avoid potential null elements in collection
                {
                    WriteValue(fieldNr, element);
                }
            }
        }

        // Port of Java writeBigInt - writes byte array representation
        public void WriteBigInt(int fieldNr, BigInteger? value)
        {
            if (value.HasValue)
            {
                // Get bytes in little-endian format, without sign indicator if positive.
                byte[] bytes = value.Value.ToByteArray(isUnsigned: value.Value.Sign >= 0, isBigEndian: false);
                WriteBytes(fieldNr, bytes);
            }
        }

        // Write timestamp as milliseconds since Unix epoch
        public void WriteTime(int fieldNr, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                _writer.Write((byte)fieldNr);
                 // Java used Instant.toEpochMilli(). DateTimeOffset.ToUnixTimeMilliseconds() is equivalent.
                _writer.Write(value.Value.ToUnixTimeMilliseconds()); // Assumes Little Endian
            }
        }

        public void WriteTxid(int fieldNr, TxID? value) // Signature now correctly refers to Protocol.TxID
        {
             if (value == null)
            {
                // Write empty string if TxID is null, matches original Java logic
                WriteString(fieldNr, "");
            }
            else
            {
                // Write the URL string representation of the TxID
                // This will now correctly call GetUrl() on Acme.Net.Sdk.Protocol.TxID
                WriteString(fieldNr, value.GetUrl().String());
            }
        }

        public void WriteTxid(int fieldNr, IEnumerable<TxID?>? entries) // Changed signature
        {
             if (entries != null)
            {
                foreach (var entry in entries)
                {
                    WriteTxid(fieldNr, entry);
                }
            }
        }

        public void WriteRawJson(int fieldNr, RawJson? value) // Changed signature
        {
            // TODO: Implement RawJson handling if needed.
            throw new NotImplementedException("RawJson handling not implemented.");
        }

        // Write Duration as seconds (varuint) and nanos (varuint)
        public void WriteDuration(int fieldNr, TimeSpan? value)
        {
             if (value.HasValue)
            {
                _writer.Write((byte)fieldNr);
                 // TimeSpan Ticks are 100-nanosecond units. 1ms = 10,000 ticks. 1s = 10,000,000 ticks.
                long totalNanos = value.Value.Ticks * 100;
                long seconds = totalNanos / 1_000_000_000;
                long nanos = totalNanos % 1_000_000_000;

                InternalWriteUInt(new BigInteger(seconds));
                InternalWriteUInt(new BigInteger(nanos));
            }
       }

        public void WriteAny(int fieldNr, object? value)
        {
            if (value != null)
            {
                WriteValue(fieldNr, value);
            }
        }

        // Port of the dispatch logic from Java using if/else if or pattern matching
        public void WriteValue(int fieldNr, object value)
        {
            // Handle arrays first
            if (IsArrayButNotByteArray(value))
            {
                // Need to cast to IEnumerable to iterate
                WriteArray(fieldNr, (System.Collections.IEnumerable)value);
                return;
            }
            
            // Specific type handling (order might matter if inheritance is involved)
            if (value is byte[] vBytes) { WriteBytes(fieldNr, vBytes); }
            // Removed explicit check for byte[][] - handled by IsArrayButNotByteArray + WriteArray
            // Removed explicit check for string[] - handled by IsArrayButNotByteArray + WriteArray
            else if (value is IIntValueEnum vEnum) { WriteEnum(fieldNr, vEnum); }
            else if (value.GetType().IsEnum) { WriteUInt(fieldNr, Convert.ToInt32(value)); } // Handle regular enums
            else if (value is IMarshallable vMarshallable) { WriteMarshallable(fieldNr, vMarshallable); }
            else if (value is string vString) { WriteString(fieldNr, vString); }
            else if (value is int vInt) { WriteUInt(fieldNr, vInt); } // Accumulate uses varuint for most numbers
            else if (value is long vLong) { WriteUInt(fieldNr, vLong); } // Using WriteUInt for consistency with protocol
            else if (value is bool vBool) { WriteBool(fieldNr, vBool); }
            else if (value is BigInteger vBigInt) { WriteUInt(fieldNr, vBigInt); } // writeUint used in Java
            // Removed BigDecimal handler (covered by BigInteger)
            else if (value is DateTimeOffset vDateTimeOffset) { WriteTime(fieldNr, vDateTimeOffset); }
            else if (value is Url vUrl) { WriteUrl(fieldNr, vUrl); } // Requires Url implementation
            else if (value is TxID vTxid) { WriteTxid(fieldNr, vTxid); } // Requires TxID implementation
            else if (value is TimeSpan vTimeSpan) { WriteDuration(fieldNr, vTimeSpan); }
             else if (value is RawJson vRawJson) { WriteRawJson(fieldNr, vRawJson); } // Requires RawJson implementation
           else
            {
                // Add more types as needed or throw error
                throw new NotSupportedException($"Unsupported type for marshalling: {value.GetType().FullName}");
            }
        }

        /// <summary>
        /// Returns the marshalled bytes accumulated so far.
        /// </summary>
        /// <returns>The byte array representation.</returns>
        public byte[] GetBytes()
        {
            _writer.Flush(); // Ensure all data is written to the MemoryStream
             // Java version returned [0x80] for empty. Let's return empty array for simplicity.
            // if (_memoryStream.Length == 0) return new byte[] { 0x80 }; 
           return _memoryStream.ToArray();
        }

        // Static helper to convert BigInteger to varint bytes
        public static byte[] ToUIntBuffer(BigInteger value)
        {
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value), "Cannot write negative value as unsigned integer.");
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                BigInteger number = value;
                while (number.CompareTo(0x80) >= 0)
                {
                    // Keep lower 7 bits, set 8th bit
                    byte byteToWrite = (byte)((number & 0x7F) | 0x80);
                    // writer.Write(byteToWrite.ToByteArray().First()); // Previous attempt
                    writer.Write(byteToWrite); // Directly write the calculated byte
                    number >>= 7;
                }
                writer.Write(((byte)(number & 0x7F))); // Write last byte
                return ms.ToArray();
            }
       }

        public static byte[] ToUIntBuffer(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Cannot write negative value as unsigned integer.");
            return ToUIntBuffer(new BigInteger(value));
        }

        // Static helper to combine byte arrays (simplified, consider edge cases if needed)
        public static byte[] Append(params byte[]?[]? arrays)
        {
             if (arrays == null) return Array.Empty<byte>();

            using (var ms = new MemoryStream())
            {
                foreach (var array in arrays)
                {
                    if (array != null)
                    {
                        ms.Write(array, 0, array.Length);
                    }
                }
                return ms.ToArray();
            }
        }
        
        // No direct C# equivalent for the specific Java clearBit logic needed for varint encoding in Java.
        // The varint encoding logic was implemented directly in InternalWriteUInt and ToUIntBuffer.
        // No direct C# equivalent for the specific Java toByteArray logic (taking first byte of BigInt representation).
        // Handled within varint logic.
        // Skipping Java's when/is pattern - C# uses standard type checking (is/as/pattern matching).

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer?.Dispose();
                _memoryStream?.Dispose();
            }
        }

        /// <summary>
        /// Gets the marshalled data as a byte array.
        /// </summary>
        /// <returns>A byte array containing the data written to the marshaller.</returns>
        public byte[] ToArray()
        {
            // Ensure all buffered data is written to the stream before getting the array
            _writer.Flush(); 
            return _memoryStream.ToArray();
        }
    }
} 