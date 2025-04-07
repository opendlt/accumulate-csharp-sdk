#nullable enable annotations

using System;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Rpc.Models;
using Acme.Net.Sdk.Transactions;
using Moq;
using Xunit;

namespace Acme.Net.Sdk.Tests.Transactions
{
    public class CreateTokenAccountBuilderTests : IDisposable
    {
        private readonly CreateTokenAccountBuilder _builder;
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly string? _originalAccApi;
        
        public CreateTokenAccountBuilderTests()
        {
            // Save original environment variable value
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            // Set test environment variable
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");
            
            // Set up mock AccountsClient using a specific endpoint to avoid environment variable issues
            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _apiClientMock.Setup(c => c.ExecuteAsync(It.IsAny<CreateTokenAccount>()))
                .ReturnsAsync(new TxResponse());
                
            _builder = new CreateTokenAccountBuilder(_apiClientMock.Object);
        }
        
        public void Dispose()
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithRequiredParameters_ReturnsCorrectCreateTokenAccountBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string accountUrl = "acc://newaccount.acme";
            string tokenUrl = "acc://token.acme";

            _builder.WithOrigin(new Url(origin));
            _builder.WithAccountUrl(new Url(accountUrl));
            _builder.WithTokenUrl(new Url(tokenUrl));

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as CreateTokenAccount;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("createTokenAccount", body.Type);
            Assert.Equal(accountUrl, body.Url.ToString());
            Assert.Equal(tokenUrl, body.TokenUrl.ToString());
            Assert.Null(body.KeyBookUrl); // Optional parameter not set
        }
        
        [Fact]
        public void BuildTransactionBody_WithAllParameters_ReturnsCorrectCreateTokenAccountBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string accountUrl = "acc://newaccount.acme";
            string tokenUrl = "acc://token.acme";
            string keyBookUrl = "acc://keybook.acme";

            _builder.WithOrigin(new Url(origin));
            _builder.WithAccountUrl(new Url(accountUrl));
            _builder.WithTokenUrl(new Url(tokenUrl));
            _builder.WithKeyBookUrl(new Url(keyBookUrl));

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as CreateTokenAccount;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("createTokenAccount", body.Type);
            Assert.Equal(accountUrl, body.Url.ToString());
            Assert.Equal(tokenUrl, body.TokenUrl.ToString());
            Assert.Equal(keyBookUrl, body.KeyBookUrl.ToString());
        }

        [Fact]
        public async Task Validate_WithoutOrigin_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithAccountUrl(new Url("acc://newaccount.acme"));
            _builder.WithTokenUrl(new Url("acc://token.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Origin must be set", exception.Message);
        }

        [Fact]
        public async Task Validate_WithoutAccountUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithTokenUrl(new Url("acc://token.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Account URL must be set", exception.Message);
        }

        [Fact]
        public async Task Validate_WithoutTokenUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithAccountUrl(new Url("acc://newaccount.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Token URL must be set", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithAccountUrl(new Url("acc://newaccount.acme"));
            _builder.WithTokenUrl(new Url("acc://token.acme"));

            // Act
            await _builder.ExecuteAsync();

            // Assert
            _apiClientMock.Verify(c => c.ExecuteAsync(It.IsAny<CreateTokenAccount>()), Times.Once);
        }

        [Fact]
        public void WithAccountUrl_WithNullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithAccountUrl((Url)null));
        }

        [Fact]
        public void WithAccountUrl_WithEmptyStringUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithAccountUrl(string.Empty));
        }

        [Fact]
        public void WithTokenUrl_WithNullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithTokenUrl((Url)null));
        }

        [Fact]
        public void WithTokenUrl_WithEmptyStringUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithTokenUrl(string.Empty));
        }

        [Fact]
        public void WithKeyBookUrl_WithNullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyBookUrl((Url)null));
        }

        [Fact]
        public void WithKeyBookUrl_WithEmptyStringUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyBookUrl(string.Empty));
        }
    }
} 