using System;
using System.Numerics;
using Xunit;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class AccountTests
    {
        [Fact]
        public void LiteTokenAccount_HasCorrectProperties()
        {
            // Arrange
            var url = Url.Parse("acc://testaccount");
            var tokenUrl = Url.Parse("acc://ACME");
            var balance = BigInteger.Parse("1000");
            
            // Act
            var account = new LiteTokenAccount(url, tokenUrl)
                .WithBalance(balance)
                .WithLockHeight(123);
            
            // Assert
            Assert.Equal(AccountType.LITE_TOKEN_ACCOUNT, account.Type);
            Assert.Equal(url, account.Url);
            Assert.Equal(tokenUrl, account.TokenUrl);
            Assert.Equal(balance, account.Balance);
            Assert.Equal(123, account.LockHeight);
        }
        
        [Fact]
        public void LiteIdentity_HasCorrectProperties()
        {
            // Arrange
            var url = Url.Parse("acc://testidentity");
            
            // Act
            var account = new LiteIdentity(url);
            
            // Assert
            Assert.Equal(AccountType.LITE_IDENTITY, account.Type);
            Assert.Equal(url, account.Url);
        }
        
        [Fact]
        public void LiteTokenAccountPrincipal_CreatesCorrectAccount()
        {
            // Arrange & Act
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var principal = new LiteTokenAccountPrincipal(keyPair);
            
            // Assert
            Assert.NotNull(principal.Account);
            Assert.IsType<LiteTokenAccount>(principal.Account);
            Assert.Equal(AccountType.LITE_TOKEN_ACCOUNT, principal.Account.Type);
            Assert.NotNull(principal.LiteTokenAccount);
            Assert.NotNull(principal.LiteTokenAccount.TokenUrl);
            Assert.Equal("acme", principal.LiteTokenAccount.TokenUrl.HostName);
            Assert.Equal(keyPair, principal.SignatureKeyPair);
        }
        
        [Fact]
        public void LiteTokenAccountPrincipal_WithTokenUrl_CreatesCorrectAccount()
        {
            // Arrange
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var tokenUrl = Url.Parse("acc://TestToken");
            
            // Act
            var principal = new LiteTokenAccountPrincipal(tokenUrl, keyPair);
            
            // Assert
            Assert.NotNull(principal.Account);
            Assert.IsType<LiteTokenAccount>(principal.Account);
            Assert.Equal(AccountType.LITE_TOKEN_ACCOUNT, principal.Account.Type);
            Assert.NotNull(principal.LiteTokenAccount);
            Assert.Equal(tokenUrl, principal.LiteTokenAccount.TokenUrl);
            Assert.Equal(keyPair, principal.SignatureKeyPair);
        }
        
        [Fact]
        public void LiteIdentityPrincipal_CreatesCorrectAccount()
        {
            // Arrange & Act
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var principal = new LiteIdentityPrincipal(keyPair);
            
            // Assert
            Assert.NotNull(principal.Account);
            Assert.IsType<LiteIdentity>(principal.Account);
            Assert.Equal(AccountType.LITE_IDENTITY, principal.Account.Type);
            Assert.NotNull(principal.LiteIdentity);
            Assert.Equal(keyPair, principal.SignatureKeyPair);
        }
        
        [Fact]
        public void LiteTokenAccountPrincipal_GenerateWithTokenUrl_CreatesCorrectAccount()
        {
            // Arrange
            var tokenUrl = Url.Parse("acc://TestToken");
            
            // Act
            var principal = LiteTokenAccountPrincipal.GenerateWithTokenUrl(tokenUrl, SignatureType.ED25519);
            
            // Assert
            Assert.NotNull(principal.Account);
            Assert.IsType<LiteTokenAccount>(principal.Account);
            Assert.Equal(AccountType.LITE_TOKEN_ACCOUNT, principal.Account.Type);
            Assert.NotNull(principal.LiteTokenAccount);
            Assert.Equal(tokenUrl, principal.LiteTokenAccount.TokenUrl);
            Assert.NotNull(principal.SignatureKeyPair);
            Assert.Equal(SignatureType.ED25519, principal.SignatureKeyPair.Type);
        }
        
        [Fact]
        public void LiteIdentityPrincipal_Generate_CreatesCorrectAccount()
        {
            // Act
            var principal = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
            
            // Assert
            Assert.NotNull(principal.Account);
            Assert.IsType<LiteIdentity>(principal.Account);
            Assert.Equal(AccountType.LITE_IDENTITY, principal.Account.Type);
            Assert.NotNull(principal.LiteIdentity);
            Assert.NotNull(principal.SignatureKeyPair);
            Assert.Equal(SignatureType.ED25519, principal.SignatureKeyPair.Type);
        }
    }
} 