#nullable disable

using System;
using System.Text;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Xunit;
using System.Threading.Tasks;
using NSec.Cryptography;

namespace Acme.Net.Sdk.Tests.Signing
{
    public class Ed25519SignatureTests
    {
        [Fact]
        public void Constructor_WithUrlAndPublicKey_SetsProperties()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var publicKey = new byte[] { 1, 2, 3, 4 };

            // Act
            var signature = new Ed25519Signature(url, publicKey);

            // Assert
            Assert.Equal(url, signature.SignerUrl);
            Assert.Equal(publicKey, signature.PublicKey);
            Assert.Equal(SignatureType.ED25519, signature.Type);
        }

        [Fact]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] publicKey = new byte[] { 1, 2, 3, 4 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Ed25519Signature(null, publicKey));
        }

        [Fact]
        public void Constructor_WithNullPublicKey_ThrowsArgumentNullException()
        {
            // Arrange
            var url = new Url("acc://test.acme");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Ed25519Signature(url, null));
        }

        [Fact]
        public void Sign_WithValidParameters_SignsData()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var signature = new Ed25519Signature();
            signature.SignerUrl = url;
            
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var txHash = new byte[32];       // txHash must be a 32-byte hash (Sign marshals the signature)
            var metadataHash = new byte[32]; // metadataHash must be a 32-byte hash of the signature metadata

            // Act
            signature.Sign(txHash, metadataHash, keyPair);

            // Assert
            Assert.Equal(txHash, signature.TransactionHash);
            Assert.NotEmpty(signature.SignatureBytes);
            Assert.Equal(keyPair.GetPublicKey(), signature.PublicKey);
        }

        [Fact(Skip = "SignatureKeyPair is ED25519-only since the key refactor; a non-ED25519 keypair cannot be constructed, so this Sign() type guard is unreachable.")]
        public void Sign_WithInvalidKeyPairType_ThrowsArgumentException()
        {
            // Arrange
            var signature = new Ed25519Signature();
            var txHash = new byte[32]; // Empty hash for testing
            var metadataHash = new byte[32]; // Empty hash for testing
            
            // Create a key pair with RCD1 type (which doesn't match ED25519 signature type)
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.RCD1);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                signature.Sign(txHash, metadataHash, keyPair));
            
            Assert.Contains("Expected key pair of type ED25519", exception.Message);
        }

        [Fact]
        public void GetInitiatorHashBuilder_ReturnsBuilderWithCorrectValues()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var publicKey = new byte[] { 1, 2, 3, 4 };
            var signature = new Ed25519Signature(url, publicKey);
            signature.Version = 2;
            signature.Timestamp = 12345;

            // Act
            var builder = signature.GetInitiatorHashBuilder();
            var hash = builder.GetCheckSum();

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void MarshalBinary_ReturnsValidByteArray()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var publicKey = new byte[32];              // Ed25519 public key is 32 bytes
            var signature = new Ed25519Signature(url, publicKey);
            signature.Version = 2;
            signature.Timestamp = 12345;
            signature.SignatureBytes = new byte[64];   // Ed25519 signature is 64 bytes
            signature.TransactionHash = new byte[32];  // 32-byte tx hash required by MarshalBinary

            // Act
            var bytes = signature.MarshalBinary();

            // Assert
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);
        }
    }
} 