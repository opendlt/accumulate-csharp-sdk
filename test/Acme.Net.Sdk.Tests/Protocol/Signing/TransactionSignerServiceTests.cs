using System;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Protocol.Signing;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Acme.Net.Sdk.Tests.Protocol.Signing
{
    public class TransactionSignerServiceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly byte[] _privateKeySeed = Hex.DecodeHex("0000000000000000000000000000000000000000000000000000000000000001");
        private readonly Transaction _sampleTransaction;

        public TransactionSignerServiceTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Create a sample transaction for testing
            var sendTokens = new SendTokens();
            sendTokens.AddRecipient("acc://recipient.acme", 100);
            
            _sampleTransaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://test.acme")),
                Body = sendTokens
            };
        }

        [Fact]
        public void Constructor_RegistersDefaultSigner()
        {
            // Act
            using var service = new TransactionSignerService();
            
            // Assert: Verify a transaction can be signed with the default ED25519 signer
            var signature = service.SignTransaction(_sampleTransaction, "ed25519", _privateKeySeed);
            
            Assert.NotNull(signature);
            Assert.Equal("ed25519", signature.Type);
            Assert.NotEmpty(signature.PublicKey);
            Assert.NotEmpty(signature.Signature);
        }
        
        [Fact]
        public void RegisterSigner_NullSigner_ThrowsArgumentNullException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RegisterSigner("test", null));
        }
        
        [Fact]
        public void RegisterSigner_EmptySignatureType_ThrowsArgumentException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            var mockSigner = new Mock<ITransactionSigner>();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.RegisterSigner("", mockSigner.Object));
            Assert.Throws<ArgumentException>(() => service.RegisterSigner("  ", mockSigner.Object));
        }
        
        [Fact]
        public void SignTransaction_NullTransaction_ThrowsArgumentNullException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                service.SignTransaction(null, "ed25519", _privateKeySeed));
        }
        
        [Fact]
        public void SignTransaction_NullPrivateKeySeed_ThrowsArgumentNullException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                service.SignTransaction(_sampleTransaction, "ed25519", null));
        }
        
        [Fact]
        public void SignTransaction_EmptySignatureType_ThrowsArgumentException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                service.SignTransaction(_sampleTransaction, "", _privateKeySeed));
            Assert.Throws<ArgumentException>(() => 
                service.SignTransaction(_sampleTransaction, "  ", _privateKeySeed));
        }
        
        [Fact]
        public void SignTransaction_UnregisteredSignatureType_ThrowsArgumentException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                service.SignTransaction(_sampleTransaction, "unknown-type", _privateKeySeed));
        }
        
        [Fact]
        public void VerifySignature_ValidSignature_ReturnsTrue()
        {
            // Arrange
            using var service = new TransactionSignerService();
            var signature = service.SignTransaction(_sampleTransaction, "ed25519", _privateKeySeed);
            
            // Act
            var result = service.VerifySignature(_sampleTransaction, signature);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void VerifySignature_ModifiedTransaction_ReturnsFalse()
        {
            // Arrange
            using var service = new TransactionSignerService();
            var signature = service.SignTransaction(_sampleTransaction, "ed25519", _privateKeySeed);
            
            // Create a modified transaction
            var modifiedSendTokens = new SendTokens();
            modifiedSendTokens.AddRecipient("acc://recipient.acme", 100);
            
            var modifiedTransaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://modified.acme")), // different principal
                Body = modifiedSendTokens
            };
            
            // Act
            var result = service.VerifySignature(modifiedTransaction, signature);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void VerifySignature_NullTransaction_ThrowsArgumentNullException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            var signature = service.SignTransaction(_sampleTransaction, "ed25519", _privateKeySeed);
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.VerifySignature(null, signature));
        }
        
        [Fact]
        public void VerifySignature_NullSignature_ThrowsArgumentNullException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.VerifySignature(_sampleTransaction, null));
        }
        
        [Fact]
        public void VerifySignature_UnregisteredSignatureType_ThrowsArgumentException()
        {
            // Arrange
            using var service = new TransactionSignerService();
            var signature = service.SignTransaction(_sampleTransaction, "ed25519", _privateKeySeed);
            
            // Create a new signature with an unregistered type
            var modifiedSignature = new TransactionSignature
            {
                Type = "unknown-type",
                Timestamp = signature.Timestamp,
                PublicKey = signature.PublicKey,
                Signature = signature.Signature,
                Signer = signature.Signer,
                SignerVersion = signature.SignerVersion,
                TransactionHash = signature.TransactionHash
            };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                service.VerifySignature(_sampleTransaction, modifiedSignature));
        }
        
        [Fact]
        public void CustomSigner_RegisteredAndUsed_WorksCorrectly()
        {
            // Arrange
            using var service = new TransactionSignerService();
            var mockSigner = new Mock<ITransactionSigner>();
            
            var expectedSignature = new TransactionSignature
            {
                Type = "custom-type",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                PublicKey = "aabbccddeeff",
                Signature = "112233445566",
                Signer = "https://example.com/custom-signer",
                SignerVersion = 1,
                TransactionHash = "998877665544"
            };
            
            mockSigner.Setup(s => s.Sign(
                    It.IsAny<Transaction>(), 
                    It.IsAny<byte[]>(), 
                    It.IsAny<long?>()))
                .Returns(expectedSignature);
                
            mockSigner.Setup(s => s.Verify(
                    It.IsAny<Transaction>(), 
                    It.IsAny<TransactionSignature>()))
                .Returns(true);
                
            // Act
            service.RegisterSigner("custom-type", mockSigner.Object);
            var signature = service.SignTransaction(_sampleTransaction, "custom-type", _privateKeySeed);
            var result = service.VerifySignature(_sampleTransaction, signature);
            
            // Assert
            Assert.Same(expectedSignature, signature);
            Assert.True(result);
            
            mockSigner.Verify(s => s.Sign(
                It.IsAny<Transaction>(), 
                It.IsAny<byte[]>(), 
                It.IsAny<long?>()), 
                Times.Once);
                
            mockSigner.Verify(s => s.Verify(
                It.IsAny<Transaction>(), 
                It.IsAny<TransactionSignature>()), 
                Times.Once);
        }
    }
} 