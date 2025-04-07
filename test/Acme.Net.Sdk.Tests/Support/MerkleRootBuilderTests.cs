using System;
using System.Text;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex conversion
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    public class MerkleRootBuilderTests
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
        public void TestGetMerkleRoot_NoHashesAdded()
        {
            var builder = new MerkleRootBuilder();
            Assert.Empty(builder.GetMerkleRoot());
        }

        [Fact]
        public void TestAddToMerkleTree_AddNullThrows()
        {
            var builder = new MerkleRootBuilder();
            Assert.Throws<ArgumentNullException>(() => builder.AddToMerkleTree(null!));
        }

        [Fact]
        public void TestMerkleRoot_SingleHash()
        {
            var builder = new MerkleRootBuilder();
            var h1 = Hash("h1");

            builder.AddToMerkleTree(h1);
            var root = builder.GetMerkleRoot();

            // Root of a single hash is the hash itself
            Assert.Equal(h1, root);
        }

        [Fact]
        public void TestMerkleRoot_TwoHashes()
        {
            var builder = new MerkleRootBuilder();
            var h1 = Hash("h1");
            var h2 = Hash("h2");

            builder.AddToMerkleTree(h1);
            builder.AddToMerkleTree(h2);
            var root = builder.GetMerkleRoot();

            // Expected root is hash(h1 + h2)
            var expectedRoot = CombineAndHash(h1, h2);
            
            Assert.Equal(expectedRoot, root);
        }

        [Fact]
        public void TestMerkleRoot_ThreeHashes()
        {
             var builder = new MerkleRootBuilder();
            var h1 = Hash("h1");
            var h2 = Hash("h2");
            var h3 = Hash("h3");

            // Add hashes incrementally
            builder.AddToMerkleTree(h1);
             // After h1: Pending=[h1]
            builder.AddToMerkleTree(h2);
            // After h2: Pending=[null, hash(h1+h2)]
            builder.AddToMerkleTree(h3);
            // After h3: Pending=[h3, hash(h1+h2)]

            var root = builder.GetMerkleRoot();

            // Expected root is hash(h3 + hash(h1+h2))
             var h1h2 = CombineAndHash(h1, h2);
            var expectedRoot = CombineAndHash(h3, h1h2); // Note: Java impl combines new hash on left

            // Let's re-verify the combination order in GetMerkleRoot()
            // It iterates _pending and combines: mdRoot = sha256(pendingHash + mdRoot)
            // So for [h3, h1h2]:
            // 1. mdRoot = h3
            // 2. mdRoot = sha256(h1h2 + h3)  <-- Order matters!
            var expectedRootCorrectOrder = CombineAndHash(h1h2, h3);

            Assert.Equal(expectedRootCorrectOrder, root);
        }

        [Fact]
        public void TestMerkleRoot_FourHashes()
        {
            var builder = new MerkleRootBuilder();
            var h1 = Hash("h1");
            var h2 = Hash("h2");
            var h3 = Hash("h3");
            var h4 = Hash("h4");

            builder.AddToMerkleTree(h1);
            builder.AddToMerkleTree(h2); // Pending = [null, h(1+2)]
            builder.AddToMerkleTree(h3); // Pending = [h3, h(1+2)]
            builder.AddToMerkleTree(h4); // Pending = [null, null, h(h(1+2)+h(3+4))]

            var root = builder.GetMerkleRoot();

            var h1h2 = CombineAndHash(h1, h2);
            var h3h4 = CombineAndHash(h3, h4);
            var expectedRoot = CombineAndHash(h1h2, h3h4); // Final combination based on AddToMerkleTree logic

            // Check GetMerkleRoot logic again: mdRoot = sha256(pendingHash + mdRoot)
            // Final state in AddToMerkleTree for 4 items: Add h4 combines with h3 => h(3+4). Then combines with h(1+2) => h(h(1+2)+h(3+4)). Pending = [null, null, h(h(1+2)+h(3+4))]
            // GetMerkleRoot iterates pending. Finds h(h(1+2)+h(3+4)). Sets mdRoot = h(h(1+2)+h(3+4)). Loop ends.
            Assert.Equal(expectedRoot, root);
        }

        // Add more tests if specific edge cases or sequences are needed
    }
} 