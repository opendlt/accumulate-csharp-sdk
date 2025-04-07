using System;
using System.Text;
using Acme.Net.Sdk.Commons.Codec;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Xunit;

namespace Acme.Net.Sdk.Tests.Commons.Codec
{
    public class HexTests
    {
        private static readonly byte[] EmptyBytes = Array.Empty<byte>();
        private static readonly char[] EmptyChars = Array.Empty<char>();
        private static readonly string EmptyString = string.Empty;

        private static readonly byte[] TestBytes = Encoding.UTF8.GetBytes("Hello World");
        private const string TestHexUpper = "48656C6C6F20576F726C64";
        private const string TestHexLower = "48656c6c6f20576f726c64";

        // === Static Decode Tests ===

        [Fact]
        public void TestDecodeHexCharArrayEmpty()
        {
            Assert.Equal(EmptyBytes, Hex.DecodeHex(EmptyChars));
        }

        [Fact]
        public void TestDecodeHexCharArrayLower()
        {
            Assert.Equal(TestBytes, Hex.DecodeHex(TestHexLower.ToCharArray()));
        }

        [Fact]
        public void TestDecodeHexCharArrayUpper()
        {
            Assert.Equal(TestBytes, Hex.DecodeHex(TestHexUpper.ToCharArray()));
        }

        [Fact]
        public void TestDecodeHexStringLower()
        {
            Assert.Equal(TestBytes, Hex.DecodeHex(TestHexLower));
        }

        [Fact]
        public void TestDecodeHexStringUpper()
        {
            Assert.Equal(TestBytes, Hex.DecodeHex(TestHexUpper));
        }

        [Fact]
        public void TestDecodeHexOddLengthStringThrows()
        {
            // Assert.Throws<DecoderException>(() => Hex.DecodeHex("48656C")); // Removed: This is even length
            Assert.Throws<DecoderException>(() => Hex.DecodeHex("0"));
            Assert.Throws<DecoderException>(() => Hex.DecodeHex("ABC"));
        }

        [Fact]
        public void TestDecodeHexOddLengthCharsThrows()
        {
            // Assert.Throws<DecoderException>(() => Hex.DecodeHex("48656C".ToCharArray())); // Removed: This is even length
             Assert.Throws<DecoderException>(() => Hex.DecodeHex("0".ToCharArray()));
             Assert.Throws<DecoderException>(() => Hex.DecodeHex("ABC".ToCharArray()));
       }

        [Fact]
        public void TestDecodeHexInvalidCharThrows()
        {
            Assert.Throws<DecoderException>(() => Hex.DecodeHex("48656X6C")); // X is invalid
            Assert.Throws<DecoderException>(() => Hex.DecodeHex("ZZ"));
        }

        // === Static Encode Tests ===

        [Fact]
        public void TestEncodeHexByteArrayEmpty()
        {
            Assert.Equal(EmptyChars, Hex.EncodeHex(EmptyBytes));
            Assert.Equal(EmptyChars, Hex.EncodeHex(EmptyBytes, true));
            Assert.Equal(EmptyChars, Hex.EncodeHex(EmptyBytes, false));
        }

        [Fact]
        public void TestEncodeHexByteArrayToLower()
        {
            Assert.Equal(TestHexLower.ToCharArray(), Hex.EncodeHex(TestBytes)); // Default is lower
            Assert.Equal(TestHexLower.ToCharArray(), Hex.EncodeHex(TestBytes, true));
        }

        [Fact]
        public void TestEncodeHexByteArrayToUpper()
        {
            Assert.Equal(TestHexUpper.ToCharArray(), Hex.EncodeHex(TestBytes, false));
        }

        [Fact]
        public void TestEncodeHexStringEmpty()
        {
            Assert.Equal(EmptyString, Hex.EncodeHexString(EmptyBytes));
            Assert.Equal(EmptyString, Hex.EncodeHexString(EmptyBytes, true));
            Assert.Equal(EmptyString, Hex.EncodeHexString(EmptyBytes, false));
        }

        [Fact]
        public void TestEncodeHexStringToLower()
        {
            Assert.Equal(TestHexLower, Hex.EncodeHexString(TestBytes)); // Default is lower
            Assert.Equal(TestHexLower, Hex.EncodeHexString(TestBytes, true));
        }

        [Fact]
        public void TestEncodeHexStringToUpper()
        {
            Assert.Equal(TestHexUpper, Hex.EncodeHexString(TestBytes, false));
        }

        // === Instance Encode/Decode Tests (using default UTF8 encoding) ===

        [Fact]
        public void TestInstanceDecodeByteArray()
        {
            var hex = new Hex(); // Default UTF-8
            var hexCharsAsBytes = Encoding.UTF8.GetBytes(TestHexLower);
            Assert.Equal(TestBytes, hex.Decode(hexCharsAsBytes));
        }

        [Fact]
        public void TestInstanceEncodeByteArray()
        {
            var hex = new Hex(); // Default UTF-8
            var expectedBytes = Encoding.UTF8.GetBytes(TestHexLower); // Instance encode defaults to lower
            Assert.Equal(expectedBytes, hex.Encode(TestBytes));
        }

        [Fact]
        public void TestInstanceDecodeObjectString()
        {
            var hex = new Hex();
            Assert.Equal(TestBytes, hex.Decode((object)TestHexLower));
        }

        [Fact]
        public void TestInstanceDecodeObjectByteArray()
        {
            var hex = new Hex(); // Default UTF-8
            var hexCharsAsBytes = Encoding.UTF8.GetBytes(TestHexLower);
            Assert.Equal(TestBytes, hex.Decode((object)hexCharsAsBytes));
        }

        [Fact]
        public void TestInstanceDecodeObjectCharArray()
        {
            var hex = new Hex();
            Assert.Equal(TestBytes, hex.Decode((object)TestHexLower.ToCharArray()));
        }

        [Fact]
        public void TestInstanceDecodeObjectInvalidTypeThrows()
        {
            var hex = new Hex();
            Assert.Throws<DecoderException>(() => hex.Decode(123));
            Assert.Throws<DecoderException>(() => hex.Decode(null!)); // Explicitly test null
        }


        [Fact]
        public void TestInstanceEncodeObjectByteArray()
        {
            var hex = new Hex();
            var expectedChars = TestHexLower.ToCharArray(); // Instance encode defaults to lower char[]
            Assert.Equal(expectedChars, (char[])hex.Encode((object)TestBytes));
        }

        [Fact]
        public void TestInstanceEncodeObjectString()
        {
            var hex = new Hex(); // Default UTF-8
            var inputString = "Hello World";
            var expectedChars = TestHexLower.ToCharArray();
            Assert.Equal(expectedChars, (char[])hex.Encode((object)inputString));
        }

        [Fact]
        public void TestInstanceEncodeObjectInvalidTypeThrows()
        {
            var hex = new Hex();
            Assert.Throws<EncoderException>(() => hex.Encode(123));
            Assert.Throws<EncoderException>(() => hex.Encode(null!)); // Explicitly test null
        }

        // === Tests with specific Encoding ===
        [Fact]
        public void TestInstanceWithAsciiEncoding()
        {
            // Use ASCII which should work fine for standard hex chars
            var hex = new Hex(Encoding.ASCII);
            var asciiHexBytes = Encoding.ASCII.GetBytes(TestHexLower);

            // Decode bytes (representing ASCII hex chars) to original bytes
            Assert.Equal(TestBytes, hex.Decode(asciiHexBytes));

            // Encode original bytes to ASCII bytes (representing hex chars)
            Assert.Equal(asciiHexBytes, hex.Encode(TestBytes));
        }

        [Fact]
        public void TestConstructorWithEncodingName()
        {
            var hex = new Hex("ASCII");
            Assert.Equal(Encoding.ASCII, hex.GetEncoding());
            Assert.Equal("us-ascii", hex.GetEncodingName(), ignoreCase: true);
        }

         [Fact]
        public void TestConstructorWithInvalidEncodingNameThrows()
        {
             Assert.Throws<NotSupportedException>(() => new Hex("Invalid-Encoding-Name"));
        }

        // === Test ToDigit (protected helper) ===
        // Can't directly test protected static, but implicitly tested by decode methods.
        // If needed, could make it internal or use reflection for specific testing.

    }
} 