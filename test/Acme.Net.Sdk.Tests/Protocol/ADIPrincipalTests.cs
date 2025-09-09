using System;
using Xunit;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using NSec.Cryptography;
using SignatureKeyPair = Acme.Net.Sdk.Signing.SignatureKeyPair;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Unit tests for the ADIPrincipal class.
    /// </summary>
    public class ADIPrincipalTests
    {
        private SignatureKeyPair CreateTestKeyPair()
        {
            // Create a test Ed25519 key pair
            var algorithm = SignatureAlgorithm.Ed25519;
            var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
            var key = Key.Create(algorithm, creationParams);
            var secretKeyBytes = key.Export(KeyBlobFormat.RawPrivateKey);
            
            // Import using the factory method
            if (!SignatureKeyPair.TryImportFromSecretKeyBytes(secretKeyBytes, SignatureType.ED25519, out var keyPair))
            {
                throw new InvalidOperationException("Failed to create test key pair");
            }
            return keyPair;
        }

        [Fact]
        public void Constructor_WithStringUrl_CreatesADIPrincipal()
        {
            // Arrange
            var adiUrl = "acc://test.acme";
            var keyPair = CreateTestKeyPair();

            // Act
            var principal = new ADIPrincipal(adiUrl, keyPair);

            // Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Account);
            Assert.Equal(AccountType.IDENTITY, principal.Account.Type);
            Assert.Equal(adiUrl, principal.Account.Url.ToString());
            Assert.Equal(keyPair, principal.SignatureKeyPair);
        }

        [Fact]
        public void Constructor_WithUrlObject_CreatesADIPrincipal()
        {
            // Arrange
            var adiUrl = Url.Parse("acc://test.acme");
            var keyPair = CreateTestKeyPair();

            // Act
            var principal = new ADIPrincipal(adiUrl, keyPair);

            // Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Account);
            Assert.Equal(AccountType.IDENTITY, principal.Account.Type);
            Assert.Equal(adiUrl.ToString(), principal.Account.Url.ToString());
            Assert.Equal(keyPair, principal.SignatureKeyPair);
        }

        [Fact]
        public void Constructor_WithNullStringUrl_ThrowsArgumentNullException()
        {
            // Arrange
            string? adiUrl = null;
            var keyPair = CreateTestKeyPair();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ADIPrincipal(adiUrl!, keyPair));
        }

        [Fact]
        public void Constructor_WithEmptyStringUrl_ThrowsArgumentException()
        {
            // Arrange
            var adiUrl = "";
            var keyPair = CreateTestKeyPair();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ADIPrincipal(adiUrl, keyPair));
        }

        [Fact]
        public void Constructor_WithWhitespaceStringUrl_ThrowsArgumentException()
        {
            // Arrange
            var adiUrl = "   ";
            var keyPair = CreateTestKeyPair();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ADIPrincipal(adiUrl, keyPair));
        }

        [Fact]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            // Arrange
            Url? adiUrl = null;
            var keyPair = CreateTestKeyPair();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ADIPrincipal(adiUrl!, keyPair));
        }

        [Fact]
        public void Constructor_WithNullKeyPair_ThrowsArgumentNullException()
        {
            // Arrange
            var adiUrl = "acc://test.acme";
            SignatureKeyPair? keyPair = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ADIPrincipal(adiUrl, keyPair!));
        }

        [Fact]
        public void ExportToBase64_ReturnsValidBase64String()
        {
            // Arrange
            var adiUrl = "acc://test.acme";
            var keyPair = CreateTestKeyPair();
            var principal = new ADIPrincipal(adiUrl, keyPair);

            // Act
            var base64String = principal.ExportToBase64();

            // Assert
            Assert.NotNull(base64String);
            Assert.NotEmpty(base64String);
            
            // Verify it's valid base64
            var bytes = Convert.FromBase64String(base64String);
            Assert.NotEmpty(bytes);
            
            // Should contain account type, signature type, and key
            Assert.True(bytes.Length > 3); // At least account type, sig type, key length, and some key data
        }

        [Fact]
        public void ImportFromBase64_WithStringUrl_RestoresADIPrincipal()
        {
            // Arrange
            var originalUrl = "acc://test.acme";
            var originalKeyPair = CreateTestKeyPair();
            var original = new ADIPrincipal(originalUrl, originalKeyPair);
            var base64String = original.ExportToBase64();

            // Act
            var imported = ADIPrincipal.ImportFromBase64(originalUrl, base64String);

            // Assert
            Assert.NotNull(imported);
            Assert.Equal(originalUrl, imported.Account.Url.ToString());
            Assert.Equal(originalKeyPair.Type, imported.SignatureKeyPair.Type);
        }

        [Fact]
        public void ImportFromBase64_WithUrlObject_RestoresADIPrincipal()
        {
            // Arrange
            var originalUrl = Url.Parse("acc://test.acme");
            var originalKeyPair = CreateTestKeyPair();
            var original = new ADIPrincipal(originalUrl, originalKeyPair);
            var base64String = original.ExportToBase64();

            // Act
            var imported = ADIPrincipal.ImportFromBase64(originalUrl, base64String);

            // Assert
            Assert.NotNull(imported);
            Assert.Equal(originalUrl.ToString(), imported.Account.Url.ToString());
            Assert.Equal(originalKeyPair.Type, imported.SignatureKeyPair.Type);
        }

        [Fact]
        public void ImportFromBase64_WithNullStringUrl_ThrowsArgumentNullException()
        {
            // Arrange
            string? adiUrl = null;
            var base64String = "dGVzdA=="; // "test" in base64

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ADIPrincipal.ImportFromBase64(adiUrl!, base64String));
        }

        [Fact]
        public void ImportFromBase64_WithEmptyStringUrl_ThrowsArgumentException()
        {
            // Arrange
            var adiUrl = "";
            var base64String = "dGVzdA==";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ADIPrincipal.ImportFromBase64(adiUrl, base64String));
        }

        [Fact]
        public void ImportFromBase64_WithNullData_ThrowsArgumentException()
        {
            // Arrange
            var adiUrl = "acc://test.acme";
            string? base64String = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ADIPrincipal.ImportFromBase64(adiUrl, base64String!));
        }

        [Fact]
        public void ImportFromBase64_WithEmptyData_ThrowsArgumentException()
        {
            // Arrange
            var adiUrl = "acc://test.acme";
            var base64String = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ADIPrincipal.ImportFromBase64(adiUrl, base64String));
        }

        [Fact]
        public void ImportFromBase64_WithInvalidBase64_ThrowsFormatException()
        {
            // Arrange
            var adiUrl = "acc://test.acme";
            var base64String = "not-valid-base64!@#$";

            // Act & Assert
            Assert.Throws<FormatException>(() => ADIPrincipal.ImportFromBase64(adiUrl, base64String));
        }
    }
}