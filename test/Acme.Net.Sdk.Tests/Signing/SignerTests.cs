using System;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Xunit;

#nullable enable annotations

namespace Acme.Net.Sdk.Tests.Signing
{
    public class SignerTests
    {
        [Fact]
        public void Prepare_WithRequiredParameters_ReturnsSignature()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

            var signer = new Signer()
                .WithUrl(url)
                .WithKeyPair(keyPair)
                .WithType(SignatureType.ED25519);

            // Act
            var signature = signer.Prepare(false);

            // Assert
            Assert.NotNull(signature);
            Assert.IsType<Ed25519Signature>(signature);
            Assert.Equal(url, ((BaseSignature)signature).SignerUrl);
            Assert.Equal(keyPair.GetPublicKey(), signature.PublicKey);
        }

        [Fact]
        public void Prepare_WithoutUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

            var signer = new Signer()
                .WithKeyPair(keyPair)
                .WithType(SignatureType.ED25519);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => signer.Prepare(false));
        }

        [Fact]
        public void Prepare_WithoutKeyPair_ThrowsInvalidOperationException()
        {
            // Arrange
            var url = new Url("acc://test.acme");

            var signer = new Signer()
                .WithUrl(url)
                .WithType(SignatureType.ED25519);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => signer.Prepare(false));
        }

        [Fact]
        public void Prepare_WithoutSignatureType_ThrowsInvalidOperationException()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

            var signer = new Signer()
                .WithUrl(url)
                .WithKeyPair(keyPair);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => signer.Prepare(false));
        }

        [Fact]
        public void Prepare_ForInitiation_RequiresVersionAndTimestamp()
        {
            // Arrange
            var url = new Url("acc://test.acme");
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

            var signer = new Signer()
                .WithUrl(url)
                .WithKeyPair(keyPair)
                .WithType(SignatureType.ED25519);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => signer.Prepare(true));

            // Add version but not timestamp
            signer.WithVersion(1);
            Assert.Throws<InvalidOperationException>(() => signer.Prepare(true));

            // Add timestamp
            signer.WithTimeStamp(12345);
            var signature = signer.Prepare(true);

            // Assert
            Assert.NotNull(signature);
            Assert.Equal(1, ((BaseSignature)signature).Version);
            Assert.Equal(12345, ((BaseSignature)signature).Timestamp);
        }

        [Fact]
        public void SignAdditional_WithValidHash_ReturnsSignedSignature()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            var hash = new byte[] { 1, 2, 3, 4, 5 };
            var signer = new Signer()
                .WithUrl(new Url("acc://example.acme"))
                .WithKeyPair(keyPair)
                .WithType(SignatureType.ED25519);

            // Act
            var signature = signer.SignAdditional(hash);

            // Assert
            Assert.NotNull(signature);
            Assert.Equal(hash, ((BaseSignature)signature).TransactionHash);
            Assert.NotEmpty(((BaseSignature)signature).SignatureBytes);
        }
    }
} 