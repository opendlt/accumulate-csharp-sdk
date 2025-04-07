using System;
using System.IO;
using System.Numerics;
using System.Text;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    public class MarshallerTests : IDisposable
    {
        private Marshaller _marshaller;

        public MarshallerTests()
        {
            _marshaller = new Marshaller();
        }

        public void Dispose()
        {
            _marshaller.Dispose();
            GC.SuppressFinalize(this);
        }

        // Helper to assert marshalled bytes against expected hex string
        private void AssertBytes(string expectedHex)
        {
            var expectedBytes = Hex.DecodeHex(expectedHex);
            var actualBytes = _marshaller.GetBytes();
            Assert.Equal(expectedBytes, actualBytes);
        }

        // === Static Method Tests ===

        [Theory]
        [InlineData(0L, "00")]
        [InlineData(1L, "01")]
        [InlineData(127L, "7f")]
        [InlineData(128L, "8001")] // 128 = 0x80 -> 0x80 | 0x00, 0x01
        [InlineData(16383L, "ff7f")] // 16383 = 0x3FFF -> 0xFF, 0x7F
        [InlineData(16384L, "808001")] // 16384 = 0x4000 -> 0x80, 0x80, 0x01
        [InlineData(long.MaxValue, "ffffffffffffffff7f")] // 2^63 - 1
        public void TestStatic_ToUIntBuffer_Long(long value, string expectedHex)
        {
            var expectedBytes = Hex.DecodeHex(expectedHex);
            var actualBytes = Marshaller.ToUIntBuffer(value);
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public void TestStatic_ToUIntBuffer_BigInteger()
        {
            // Test value larger than long.MaxValue (2^64)
            BigInteger largeValue = BigInteger.Pow(2, 64);
            // Expected for 2^64: 9 continuation bytes (9*7=63 bits) + 1 byte with remaining bit(s)
            // Final byte should represent the 64th bit (value 2 in the lowest bits of the 10th byte)
            string expectedHex = "80808080808080808002"; // Corrected expected value
            var expectedBytes = Hex.DecodeHex(expectedHex);
            var actualBytes = Marshaller.ToUIntBuffer(largeValue);
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public void TestStatic_ToUIntBuffer_NegativeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Marshaller.ToUIntBuffer(-1L));
            Assert.Throws<ArgumentOutOfRangeException>(() => Marshaller.ToUIntBuffer(BigInteger.MinusOne));
        }

        [Fact]
        public void TestStatic_Append()
        {
            byte[] a1 = { 1, 2 };
            byte[] a2 = { 3, 4, 5 };
            byte[]? a3 = null;
            byte[] a4 = { 6 };
            byte[] expected = { 1, 2, 3, 4, 5, 6 };
            Assert.Equal(expected, Marshaller.Append(a1, a2, a3, a4));
            Assert.Empty(Marshaller.Append(null));
             Assert.Empty(Marshaller.Append());
       }

        // === Instance Method Tests ===

        [Fact]
        public void TestWriteString()
        {
            _marshaller.WriteString(1, "hello");
            // Field 1, Length 5 (0x05), "hello" (UTF8: 68 65 6c 6c 6f)
            AssertBytes("010568656c6c6f");
        }

         [Fact]
        public void TestWriteString_Empty()
        {
            _marshaller.WriteString(1, "");
            // Field 1, Length 0 (0x00)
            AssertBytes("0100");
        }
        
        [Fact]
        public void TestWriteString_Null()
        {
            _marshaller.WriteString(1, (string?)null);
            Assert.Empty(_marshaller.GetBytes()); // Null strings are ignored
        }

         [Fact]
        public void TestWriteBool_True()
        {
            _marshaller.WriteBool(2, true);
            // Field 2, Value 1 (0x01)
            AssertBytes("0201");
        }
        
        [Fact]
        public void TestWriteBool_False()
        {
            _marshaller.WriteBool(2, false);
             // Field 2, Value 0 (0x00)
           AssertBytes("0200");
        }

        [Fact]
        public void TestWriteUInt_Small()
        {
            _marshaller.WriteUInt(3, 100L); // 100 = 0x64
            // Field 3, Value 100 (0x64)
            AssertBytes("0364");
        }
        
         [Fact]
        public void TestWriteUInt_Large()
        {
             _marshaller.WriteUInt(3, 16384L); // 16384 needs 3 bytes
            // Field 3, Value 16384 (0x80 0x80 0x01)
            AssertBytes("03808001");
        }

        [Fact]
        public void TestWriteBytes()
        {
            byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF };
            _marshaller.WriteBytes(4, data);
            // Field 4, Length 4 (0x04), Data (DE AD BE EF)
            AssertBytes("0404deadbeef");
        }
        
        [Fact]
        public void TestWriteHash()
        {
            // Hashes are written directly without length prefix
            byte[] hash = Hex.DecodeHex("aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899");
             _marshaller.WriteHash(5, hash);
            // Field 5, Hash data
           AssertBytes("05aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899");
        }

        [Fact]
        public void TestWriteValue_Dispatch()
        {
             _marshaller.WriteValue(1, "test"); // String
             _marshaller.WriteValue(2, true); // Bool
            _marshaller.WriteValue(3, 128L); // Long -> UInt
            _marshaller.WriteValue(4, new byte[]{1,2,3}); // byte[] -> Bytes
            
            // Expected: (f1, l4, test) (f2, 1) (f3, 0x80 0x01) (f4, l3, 010203)
            AssertBytes("010474657374" + "0201" + "038001" + "0403010203");
        }

         [Fact]
        public void TestWriteValue_UnsupportedThrows()
        {
            // DateTime is not handled by default
             Assert.Throws<NotSupportedException>(() => _marshaller.WriteValue(99, DateTime.Now));
        }
        
        // TODO: Add tests for WriteInt, WriteLong, WriteTime, WriteDuration when endianness and format are confirmed.
        // TODO: Add tests for WriteBigInt when format is confirmed.
        // TODO: Add tests for WriteArray.
        // TODO: Add tests for stubbed methods once dependencies are ported (WriteUrl, WriteEnum, WriteMarshallable, WriteTxid, WriteRawJson).
    }
} 