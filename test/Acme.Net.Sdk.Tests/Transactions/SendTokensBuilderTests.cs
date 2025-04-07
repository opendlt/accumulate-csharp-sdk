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
    public class SendTokensBuilderTests : IDisposable
    {
        private readonly SendTokensBuilder _builder;
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly string? _originalAccApi;
        
        public SendTokensBuilderTests()
        {
            // Save original environment variable value
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            // Set test environment variable
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");
            
            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _builder = new SendTokensBuilder(_apiClientMock.Object);
        }
        
        public void Dispose()
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithValidParameters_ReturnsCorrectSendTokensBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string recipient = "acc://recipient.acme";
            ulong amount = 100;

            _builder.WithOrigin(new Url(origin));
            _builder.AddRecipient(new Url(recipient), amount);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as SendTokens;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("sendTokens", body.Type);
            Assert.Single(body.Recipients);
            Assert.Equal(recipient, body.Recipients[0].Url.ToString());
            Assert.Equal(amount, body.Recipients[0].Amount);
        }

        [Fact]
        public async Task Validate_WithoutOrigin_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.AddRecipient(new Url("acc://recipient.acme"), 100);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Origin must be set", exception.Message);
        }

        [Fact]
        public async Task Validate_WithoutRecipients_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("At least one recipient must be specified", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.AddRecipient(new Url("acc://recipient.acme"), 100);

            // Act
            await _builder.ExecuteAsync();

            // Assert
            _apiClientMock.Verify(c => c.ExecuteAsync(It.IsAny<SendTokens>()), Times.Once);
        }

        [Fact]
        public void AddRecipient_WithNullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.AddRecipient((Url)null, 100));
        }

        [Fact]
        public void AddRecipient_WithEmptyStringUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.AddRecipient(string.Empty, 100));
        }

        [Fact]
        public void WithHash_WithNullHash_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithHash(null));
        }

        [Fact]
        public void WithMeta_WithNullMeta_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithMeta(null));
        }
    }
} 