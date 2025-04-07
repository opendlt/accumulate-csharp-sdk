using System;
using System.Text;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex conversion
using Acme.Net.Sdk.Protocol; // Added for Url class
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    public class HashBuilderTests
    {
        // Helper to create a simple hash from a string
        private byte[] Hash(string input)
        {
            return HashUtils.Sha256(Encoding.UTF8.GetBytes(input));
        }

        // Helper to combine and hash two byte arrays
        private byte[] CombineAndHash(byte[] left, byte[] right)
        {
             byte[] combined = new byte[left.Length + right.Length];
             System.Buffer.BlockCopy(left, 0, combined, 0, left.Length);
             System.Buffer.BlockCopy(right, 0, combined, left.Length, right.Length);
             return HashUtils.Sha256(combined);
        }

        [Fact]
        public void TestMerkleHash_Empty()
        {
            var builder = new HashBuilder();
            Assert.Empty(builder.MerkleHash());
        }

        [Fact]
        public void TestGetCheckSum_Empty()
        {
            var builder = new HashBuilder();
            // Checksum of empty is hash of empty
            var expectedChecksum = HashUtils.Sha256(Array.Empty<byte>());
            Assert.Equal(expectedChecksum, builder.GetCheckSum());
        }

        [Fact]
        public void TestAddHash()
        {
            var builder = new HashBuilder();
            var h1 = Hash("data1");
            builder.AddHash(h1);
            // Check Merkle Root (should be h1)
            Assert.Equal(h1, builder.MerkleHash());
            // Check Checksum (should be hash(h1))
            Assert.Equal(HashUtils.Sha256(h1), builder.GetCheckSum());
        }

        [Fact]
        public void TestAddBytes()
        {
            var builder = new HashBuilder();
            var data1 = Encoding.UTF8.GetBytes("data1");
            var h1 = HashUtils.Sha256(data1);
            builder.AddBytes(data1);
            // Check Merkle Root (should be h1)
            Assert.Equal(h1, builder.MerkleHash());
            // Check Checksum (should be hash(h1))
            Assert.Equal(HashUtils.Sha256(h1), builder.GetCheckSum());
        }

        [Fact]
        public void TestAddUrl_Placeholder()
        {
            var builder = new HashBuilder();
            var urlString = "acc://some-url";
            var urlObject = Url.Parse(urlString);
            var urlBytes = Encoding.UTF8.GetBytes(urlObject.String());
            var hUrl = HashUtils.Sha256(urlBytes);
            builder.AddUrl(urlObject);
            // Check Merkle Root (should be hUrl)
            Assert.Equal(hUrl, builder.MerkleHash());
            // Check Checksum (should be hash(hUrl))
            Assert.Equal(HashUtils.Sha256(hUrl), builder.GetCheckSum());
        }

        [Fact]
        public void TestMultipleAdds_MerkleHash()
        {
            var builder = new HashBuilder();
            var data1 = Encoding.UTF8.GetBytes("data1");
            var data2 = Encoding.UTF8.GetBytes("data2");
            var h1 = HashUtils.Sha256(data1); // Added via AddBytes
            var h2 = Hash("hash2"); // Added via AddHash

            builder.AddBytes(data1).AddHash(h2);

            // Merkle root should be Hash(h1 + h2)
            var expectedRoot = CombineAndHash(h1, h2);
            Assert.Equal(expectedRoot, builder.MerkleHash());
        }

        [Fact]
        public void TestMultipleAdds_Checksum()
        {
            var builder = new HashBuilder();
            var data1 = Encoding.UTF8.GetBytes("data1");
            var data2 = Encoding.UTF8.GetBytes("data2");
            var h1 = HashUtils.Sha256(data1); // Added via AddBytes
            var h2 = Hash("hash2"); // Added via AddHash

            builder.AddBytes(data1).AddHash(h2);

            // Checksum should be Hash(h1 + h2 concatenated)
            var combinedHashes = new byte[h1.Length + h2.Length];
            System.Buffer.BlockCopy(h1, 0, combinedHashes, 0, h1.Length);
            System.Buffer.BlockCopy(h2, 0, combinedHashes, h1.Length, h2.Length);
            var expectedChecksum = HashUtils.Sha256(combinedHashes);

            Assert.Equal(expectedChecksum, builder.GetCheckSum());
        }

        [Fact]
        public void TestAddIgnoresNullAndEmpty()
        {
            var builder = new HashBuilder();
            var h1 = Hash("data1");

            builder.AddHash(null)
                   .AddHash(Array.Empty<byte>())
                   .AddBytes(null)
                   .AddBytes(Array.Empty<byte>())
                   .AddUrl(null)
                   .AddHash(h1); // Add one valid hash
                   
            // Merkle root should just be h1
            Assert.Equal(h1, builder.MerkleHash());
            // Checksum should be hash(h1)
             Assert.Equal(HashUtils.Sha256(h1), builder.GetCheckSum());
        }

        [Fact]
        public void TestUIntAndLongMethods()
        {
            var builder = new HashBuilder();
            
            // Test AddUInt with integer
            var hashBeforeInt = builder.MerkleHash();
            builder.AddUInt(123);
            var hashAfterInt = builder.MerkleHash();
            Assert.NotEqual(hashBeforeInt, hashAfterInt); // Hash should change after adding
            
            // Test AddLong
            var hashBeforeLong = builder.MerkleHash();
            builder.AddLong(456L);
            var hashAfterLong = builder.MerkleHash();
            Assert.NotEqual(hashBeforeLong, hashAfterLong); // Hash should change after adding
            
            // Test null values
            var hashBefore = builder.MerkleHash();
            builder.AddUInt(null);
            builder.AddLong(null);
            var hashAfter = builder.MerkleHash();
            Assert.Equal(hashBefore, hashAfter); // Hash should not change when adding nulls
        }
    }
} 