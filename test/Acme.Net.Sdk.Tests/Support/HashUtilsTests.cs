using System.Text;
using Acme.Net.Sdk.Support;
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    public class HashUtilsTests
    {
        [Fact]
        public void TestSha256_KnownValue()
        {
            var input = Encoding.UTF8.GetBytes("hello world");
            // Known SHA-256 hash for "hello world"
            var expectedHashHex = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";
            var expectedHash = Acme.Net.Sdk.Commons.Codec.Binary.Hex.DecodeHex(expectedHashHex);

            var actualHash = HashUtils.Sha256(input);

            Assert.Equal(expectedHash, actualHash);
        }

        [Fact]
        public void TestSha256_EmptyInput()
        {
            var input = System.Array.Empty<byte>();
            // Known SHA-256 hash for empty input
            var expectedHashHex = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            var expectedHash = Acme.Net.Sdk.Commons.Codec.Binary.Hex.DecodeHex(expectedHashHex);

            var actualHash = HashUtils.Sha256(input);

            Assert.Equal(expectedHash, actualHash);
        }

        [Fact]
        public void TestSha256_NullInputThrows()
        {
            Assert.Throws<System.ArgumentNullException>(() => HashUtils.Sha256(null!));
        }

        [Fact]
        public void TestSha512_KnownValue()
        {
            var input = Encoding.UTF8.GetBytes("hello world");
            // Known SHA-512 hash for "hello world"
            var expectedHashHex = "309ecc489c12d6eb4cc40f50c902f2b4d0ed77ee511a7c7a9bcd3ca86d4cd86f989dd35bc5ff499670da34255b45b0cfd830e81f605dcf7dc5542e93ae9cd76f";
            var expectedHash = Acme.Net.Sdk.Commons.Codec.Binary.Hex.DecodeHex(expectedHashHex);

            var actualHash = HashUtils.Sha512(input);

            Assert.Equal(expectedHash, actualHash);
        }

        [Fact]
        public void TestSha512_EmptyInput()
        {
            var input = System.Array.Empty<byte>();
            // Known SHA-512 hash for empty input
            var expectedHashHex = "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e";
            var expectedHash = Acme.Net.Sdk.Commons.Codec.Binary.Hex.DecodeHex(expectedHashHex);

            var actualHash = HashUtils.Sha512(input);

            Assert.Equal(expectedHash, actualHash);
        }

        [Fact]
        public void TestSha512_NullInputThrows()
        {
            Assert.Throws<System.ArgumentNullException>(() => HashUtils.Sha512(null!));
        }
    }
} 