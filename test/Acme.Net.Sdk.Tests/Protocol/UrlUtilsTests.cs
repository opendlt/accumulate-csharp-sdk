using System;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Signing;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class UrlUtilsTests
    {
        [Fact]
        public void ComputeLiteIdentityUrl_WithValidPublicKey_ReturnsCorrectUrl()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            byte[] publicKey = keyPair.GetPublicKey();
            
            // Act - explicitly call method with byte[] parameter
            var url = UrlUtils.ComputeLiteIdentityUrl(publicKey);
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(48, url.Authority.Length); // 40 chars for hash + 8 for checksum
            Assert.Empty(url.Path.Trim('/'));
        }
        
        [Fact]
        public void ComputeLiteIdentityUrl_WithKeyPair_ReturnsCorrectUrl()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            byte[] publicKey = keyPair.GetPublicKey();
            
            // Act - explicitly call method with byte[] parameter 
            var url = UrlUtils.ComputeLiteIdentityUrl(publicKey);
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(48, url.Authority.Length);
            Assert.Empty(url.Path.Trim('/'));
        }
        
        [Fact]
        public void ComputeLiteTokenAccountUrl_WithValidInputs_ReturnsCorrectUrl()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            byte[] publicKey = keyPair.GetPublicKey();
            var tokenUrl = Url.Parse("acc://ACME");
            
            Console.WriteLine($"Token URL: {tokenUrl}");
            Console.WriteLine($"Token URL Authority: {tokenUrl.Authority}");
            Console.WriteLine($"Token URL HostName: {tokenUrl.HostName}");
            Console.WriteLine($"Token URL Path: {tokenUrl.Path}");
            
            // Act - explicitly call method with byte[] parameter
            var url = UrlUtils.ComputeLiteTokenAccountUrl(publicKey, tokenUrl);
            
            Console.WriteLine($"Result URL: {url}");
            Console.WriteLine($"Result URL Authority: {url.Authority}");
            Console.WriteLine($"Result URL HostName: {url.HostName}");
            Console.WriteLine($"Result URL Path: {url.Path}");
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(48, url.Authority.Length);
            Assert.Contains("/acme", url.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public void ComputeLiteTokenAccountUrl_WithKeyPair_ReturnsCorrectUrl()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            byte[] publicKey = keyPair.GetPublicKey();
            var tokenUrl = Url.Parse("acc://ACME");
            
            // Act - explicitly call method with byte[] parameter
            var url = UrlUtils.ComputeLiteTokenAccountUrl(publicKey, tokenUrl);
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(48, url.Authority.Length);
            Assert.Contains("/acme", url.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public void ComputeAcmeLiteTokenAccountUrl_WithPublicKey_ReturnsCorrectUrl()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            byte[] publicKey = keyPair.GetPublicKey();
            
            // Act - explicitly call method with byte[] parameter
            var url = UrlUtils.ComputeAcmeLiteTokenAccountUrl(publicKey);
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(48, url.Authority.Length);
            Assert.Contains("/acme", url.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public void ComputeAcmeLiteTokenAccountUrl_WithKeyPair_ReturnsCorrectUrl()
        {
            // Arrange
            var keyPair = new Ed25519SignatureKeyPair();
            byte[] publicKey = keyPair.GetPublicKey();
            
            // Act - explicitly call method with byte[] parameter
            var url = UrlUtils.ComputeAcmeLiteTokenAccountUrl(publicKey);
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(48, url.Authority.Length);
            Assert.Contains("/acme", url.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public void ComputeLiteDataAccountUrl_WithData_ReturnsCorrectUrl()
        {
            // Arrange
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Test Data for Hashing");
            
            // Act
            var url = UrlUtils.ComputeLiteDataAccountUrl(data);
            
            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("acc://", url.ToString());
            Assert.Equal(64, url.Authority.Length); // Full SHA256 hash is 64 hex chars
            Assert.Empty(url.Path.Trim('/'));
        }
        
        [Fact]
        public void FormatAdiUrl_WithValidName_ReturnsCorrectUrl()
        {
            // Arrange
            string adiName = "myadi";
            
            // Act
            var url = UrlUtils.FormatAdiUrl(adiName);
            
            // Assert
            Assert.NotNull(url);
            Assert.Equal("acc://myadi.acme", url.ToString());
        }
        
        [Fact]
        public void FormatTokenAccountUrl_WithValidInputs_ReturnsCorrectUrl()
        {
            // Arrange
            var adiUrl = Url.Parse("acc://myadi.acme");
            string accountName = "tokens";
            
            // Act
            var url = UrlUtils.FormatTokenAccountUrl(adiUrl, accountName);
            
            // Assert
            Assert.NotNull(url);
            Assert.Equal("acc://myadi.acme/tokens", url.ToString());
        }
        
        [Fact]
        public void FormatDataAccountUrl_WithValidInputs_ReturnsCorrectUrl()
        {
            // Arrange
            var adiUrl = Url.Parse("acc://myadi.acme");
            string accountName = "data";
            
            // Act
            var url = UrlUtils.FormatDataAccountUrl(adiUrl, accountName);
            
            // Assert
            Assert.NotNull(url);
            Assert.Equal("acc://myadi.acme/data", url.ToString());
        }
        
        [Fact]
        public void FormatKeyBookUrl_WithValidInputs_ReturnsCorrectUrl()
        {
            // Arrange
            var adiUrl = Url.Parse("acc://myadi.acme");
            string bookName = "book";
            
            // Act
            var url = UrlUtils.FormatKeyBookUrl(adiUrl, bookName);
            
            // Assert
            Assert.NotNull(url);
            Assert.Equal("acc://myadi.acme/book", url.ToString());
        }
        
        [Fact]
        public void FormatKeyPageUrl_WithValidInputs_ReturnsCorrectUrl()
        {
            // Arrange
            var bookUrl = Url.Parse("acc://myadi.acme/book");
            string pageName = "page1";
            
            // Act
            var url = UrlUtils.FormatKeyPageUrl(bookUrl, pageName);
            
            // Assert
            Assert.NotNull(url);
            Assert.Equal("acc://myadi.acme/book/page1", url.ToString());
        }
        
        [Fact]
        public void IsLiteIdentityUrl_WithValidLiteIdentityUrl_ReturnsTrue()
        {
            // Arrange - use public key directly
            var keyPair = new Ed25519SignatureKeyPair();
            var url = UrlUtils.ComputeLiteIdentityUrl(keyPair.GetPublicKey());
            
            // Act
            bool result = UrlUtils.IsLiteIdentityUrl(url);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void IsLiteIdentityUrl_WithNonLiteIdentityUrl_ReturnsFalse()
        {
            // Arrange
            var url = Url.Parse("acc://myadi.acme");
            
            // Act
            bool result = UrlUtils.IsLiteIdentityUrl(url);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void IsLiteTokenAccountUrl_WithValidLiteTokenAccountUrl_ReturnsTrue()
        {
            // Arrange - use public key directly
            var keyPair = new Ed25519SignatureKeyPair();
            var url = UrlUtils.ComputeAcmeLiteTokenAccountUrl(keyPair.GetPublicKey());
            
            // Act
            bool result = UrlUtils.IsLiteTokenAccountUrl(url);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void IsLiteTokenAccountUrl_WithNonLiteTokenAccountUrl_ReturnsFalse()
        {
            // Arrange
            var url = Url.Parse("acc://myadi.acme/tokens");
            
            // Act
            bool result = UrlUtils.IsLiteTokenAccountUrl(url);
            
            // Assert
            Assert.False(result);
        }
    }
} 