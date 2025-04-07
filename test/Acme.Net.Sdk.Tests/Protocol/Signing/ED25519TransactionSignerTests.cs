using System;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Protocol.Signing;
using Xunit;
using Xunit.Abstractions;

namespace Acme.Net.Sdk.Tests.Protocol.Signing
{
    public class ED25519TransactionSignerTests
    {
        private readonly ITestOutputHelper _output;
        
        // The private key seed provided
        private readonly byte[] _keySeed = Hex.DecodeHex("a2fd3e3b8c130edac176da83dcf809e22a01ab5a853560806e6cc054b3e160b0");
        
        // Sample transaction for testing
        private readonly Transaction _sampleTransaction;
        
        public ED25519TransactionSignerTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Create a sample transaction for testing
            _sampleTransaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://adi.acme")),
                Body = new CreateTokenAccount()
                    .WithUrl(new Url("acc://adi.acme/ACME"))
                    .WithTokenUrl(new Url("acc://ACME.acme"))
            };
        }
        
        [Fact]
        public void Sign_ValidTransaction_ReturnsValidSignature()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            long timestamp = 1234567890; // Fixed timestamp for testing
            
            // Act
            var signature = signer.Sign(_sampleTransaction, _keySeed, timestamp);
            
            // Assert
            Assert.NotNull(signature);
            Assert.Equal("ed25519", signature.Type);
            Assert.Equal(timestamp, signature.Timestamp);
            Assert.NotEmpty(signature.PublicKey);
            Assert.NotEmpty(signature.Signature);
            Assert.NotEmpty(signature.TransactionHash);
            
            // The public key should match the expected value from the key seed
            string expectedPublicKey = "e55d973bf691381c94602354d1e1f655f7b1c4bd56760dffeffa2bef4541ec11";
            Assert.Equal(expectedPublicKey, signature.PublicKey);
            
            _output.WriteLine($"Public Key: {signature.PublicKey}");
            _output.WriteLine($"Transaction Hash: {signature.TransactionHash}");
            _output.WriteLine($"Signature: {signature.Signature}");
        }
        
        [Fact]
        public void Verify_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            long timestamp = 1234567890; // Fixed timestamp for testing
            
            // Act
            var signature = signer.Sign(_sampleTransaction, _keySeed, timestamp);
            bool isValid = signer.Verify(_sampleTransaction, signature);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void Verify_ModifiedTransaction_ReturnsFalse()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            long timestamp = 1234567890; // Fixed timestamp for testing
            
            // Sign the original transaction
            var signature = signer.Sign(_sampleTransaction, _keySeed, timestamp);
            
            // Create a modified version of the transaction
            var modifiedTransaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://modified.acme")), // Changed principal
                Body = new CreateTokenAccount()
                    .WithUrl(new Url("acc://adi.acme/ACME"))
                    .WithTokenUrl(new Url("acc://ACME.acme"))
            };
            
            // Act
            bool isValid = signer.Verify(modifiedTransaction, signature);
            
            // Assert
            Assert.False(isValid);
        }
        
        [Fact]
        public void Sign_NullTransaction_ThrowsArgumentNullException()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => signer.Sign(null, _keySeed));
        }
        
        [Fact]
        public void Sign_NullPrivateKeySeed_ThrowsArgumentNullException()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => signer.Sign(_sampleTransaction, null));
        }
        
        [Fact]
        public void Verify_NullTransaction_ThrowsArgumentNullException()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            var signature = signer.Sign(_sampleTransaction, _keySeed);
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => signer.Verify(null, signature));
        }
        
        [Fact]
        public void Verify_NullSignature_ThrowsArgumentNullException()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => signer.Verify(_sampleTransaction, null));
        }
        
        [Fact]
        public void Verify_UnsupportedSignatureType_ThrowsArgumentException()
        {
            // Arrange
            var signer = new ED25519TransactionSigner();
            var signature = signer.Sign(_sampleTransaction, _keySeed);
            signature.Type = "unsupported"; // Change the signature type
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => signer.Verify(_sampleTransaction, signature));
        }
    }
} 