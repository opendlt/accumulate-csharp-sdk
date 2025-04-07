using System;
using System.Numerics;
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
    public class IssueTokensBuilderTests : IDisposable
    {
        private readonly IssueTokensBuilder _builder;
        private readonly Mock<TokensClient> _mockApiClient;
        private readonly string _originalAccApi;
        
        public IssueTokensBuilderTests()
        {
            // Save original environment variable value
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            // Set test environment variable
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");
            
            // Set up mock TokensClient using a specific endpoint to avoid environment variable issues
            _mockApiClient = new Mock<TokensClient>(new Uri("http://test-endpoint.com"));
            _mockApiClient.Setup(c => c.ExecuteAsync(It.IsAny<IssueTokens>()))
                .ReturnsAsync(new TxResponse());
                
            _builder = new IssueTokensBuilder(_mockApiClient.Object);
        }
        
        public void Dispose()
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithSingleRecipient_ReturnsCorrectIssueTokensBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string recipient = "acc://recipient.acme";
            ulong amount = 100;

            _builder.WithOrigin(new Url(origin));
            _builder.WithRecipient(new Url(recipient));
            _builder.WithAmount(amount);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as IssueTokens;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("issueTokens", body.Type);
            Assert.Equal(recipient, body.Recipient.ToString());
            Assert.Equal(new BigInteger(amount), body.Amount);
            Assert.Null(body.Recipients);
        }

        [Fact]
        public void BuildTransactionBody_WithMultipleRecipients_ReturnsCorrectIssueTokensBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string recipient1 = "acc://recipient1.acme";
            string recipient2 = "acc://recipient2.acme";
            ulong amount1 = 100;
            ulong amount2 = 200;

            _builder.WithOrigin(new Url(origin));
            _builder.AddRecipient(new Url(recipient1), amount1);
            _builder.AddRecipient(new Url(recipient2), amount2);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as IssueTokens;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("issueTokens", body.Type);
            Assert.NotNull(body.Recipients);
            Assert.Equal(2, body.Recipients.Count);
            
            Assert.Equal(recipient1, body.Recipients[0].Url.ToString());
            Assert.Equal(amount1, body.Recipients[0].Amount);
            
            Assert.Equal(recipient2, body.Recipients[1].Url.ToString());
            Assert.Equal(amount2, body.Recipients[1].Amount);
        }

        [Fact]
        public void Validate_WithoutOrigin_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithRecipient(new Url("acc://recipient.acme"));
            _builder.WithAmount(100);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _builder.ExecuteAsync().GetAwaiter().GetResult());
            Assert.Contains("Origin must be set", exception.Message);
        }

        [Fact]
        public void Validate_WithoutRecipient_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _builder.ExecuteAsync().GetAwaiter().GetResult());
            Assert.Contains("Recipient must be specified", exception.Message);
        }

        [Fact]
        public void Validate_WithMultipleRecipients_ButNoRecipients_ThrowsInvalidOperationException()
        {
            // Arrange - trick the builder into thinking it's in multiple recipients mode
            var field = _builder.GetType().GetField("_useMultipleRecipients", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_builder, true);
            
            _builder.WithOrigin(new Url("acc://origin.acme"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _builder.ExecuteAsync().GetAwaiter().GetResult());
            Assert.Contains("At least one recipient must be specified for multiple recipients mode", exception.Message);
        }

        [Fact]
        public void Validate_WithZeroAmount_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithRecipient(new Url("acc://recipient.acme"));
            _builder.WithAmount(0);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _builder.ExecuteAsync().GetAwaiter().GetResult());
            Assert.Contains("Amount must be greater than zero", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithRecipient(new Url("acc://recipient.acme"));
            _builder.WithAmount(100);

            // Act
            await _builder.ExecuteAsync();

            // Assert
            _mockApiClient.Verify(c => c.ExecuteAsync(It.IsAny<IssueTokens>()), Times.Once);
        }

        [Fact]
        public void WithRecipient_WithNullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithRecipient((Url)null));
        }

        [Fact]
        public void WithRecipient_WithEmptyStringUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithRecipient(string.Empty));
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
    }
} 