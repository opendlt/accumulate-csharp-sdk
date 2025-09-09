using System;
using Xunit;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Unit tests for the ADI (Accumulate Digital Identity) class.
    /// </summary>
    public class ADITests
    {
        [Fact]
        public void Constructor_Default_CreatesADI()
        {
            // Act
            var adi = new ADI();

            // Assert
            Assert.NotNull(adi);
            Assert.Equal(AccountType.IDENTITY, adi.Type);
        }

        [Fact]
        public void Constructor_WithUrl_SetsUrlProperty()
        {
            // Arrange
            var url = Url.Parse("acc://myidentity.acme");

            // Act
            var adi = new ADI(url);

            // Assert
            Assert.NotNull(adi);
            Assert.Equal(AccountType.IDENTITY, adi.Type);
            Assert.Equal(url, adi.Url);
            Assert.Equal("acc://myidentity.acme", adi.Url.ToString());
        }

        [Fact]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            // Arrange
            Url? url = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ADI(url!));
        }

        [Fact]
        public void Type_AlwaysReturnsIDENTITY()
        {
            // Arrange
            var adi1 = new ADI();
            var adi2 = new ADI(Url.Parse("acc://test.acme"));

            // Act & Assert
            Assert.Equal(AccountType.IDENTITY, adi1.Type);
            Assert.Equal(AccountType.IDENTITY, adi2.Type);
        }

        [Fact]
        public void MarshalBinary_ReturnsValidBytes()
        {
            // Arrange
            var url = Url.Parse("acc://identity.acme");
            var adi = new ADI(url);

            // Act
            var bytes = adi.MarshalBinary();

            // Assert
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);
            
            // Should contain at least type and url fields
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void MarshalBinary_WithoutUrl_StillReturnsBytes()
        {
            // Arrange
            var adi = new ADI();

            // Act
            var bytes = adi.MarshalBinary();

            // Assert
            Assert.NotNull(bytes);
            // Should at least contain the type field
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void ToString_ReturnsUrlString()
        {
            // Arrange
            var url = Url.Parse("acc://myidentity.acme");
            var adi = new ADI(url);

            // Act
            var result = adi.ToString();

            // Assert
            Assert.Equal("acc://myidentity.acme", result);
        }

        [Fact]
        public void ToString_WithoutUrl_ReturnsDefaultMessage()
        {
            // Arrange
            var adi = new ADI();

            // Act
            var result = adi.ToString();

            // Assert
            Assert.Equal("Account (no URL)", result);
        }

        [Fact]
        public void ADI_ImplementsIAccount()
        {
            // Arrange
            var adi = new ADI();

            // Act & Assert
            Assert.IsAssignableFrom<IAccount>(adi);
        }

        [Fact]
        public void ADI_InheritsFromAccount()
        {
            // Arrange
            var adi = new ADI();

            // Act & Assert
            Assert.IsAssignableFrom<Account>(adi);
        }

        [Fact]
        public void Url_CanBeSetAfterConstruction()
        {
            // Arrange
            var adi = new ADI();
            var url = Url.Parse("acc://newidentity.acme");

            // Act
            adi.Url = url;

            // Assert
            Assert.Equal(url, adi.Url);
            Assert.Equal("acc://newidentity.acme", adi.Url.ToString());
        }

        [Fact]
        public void Url_SettingNull_ThrowsArgumentNullException()
        {
            // Arrange
            var adi = new ADI(Url.Parse("acc://test.acme"));

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => adi.Url = null!);
        }
    }
}