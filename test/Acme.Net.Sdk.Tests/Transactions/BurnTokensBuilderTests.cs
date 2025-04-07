using System;
using System.Numerics;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Transactions;
using Moq;
using Xunit;

namespace Acme.Net.Sdk.Tests.Transactions
{
    public class BurnTokensBuilderTests : IDisposable
    {
        private readonly BurnTokensBuilder _builder;
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly string? _originalAccApi;
        
        public BurnTokensBuilderTests()
        {
            // Save original environment variable value
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            // Set test environment variable
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");
            
            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _builder = new BurnTokensBuilder(_apiClientMock.Object);
        }
        
        public void Dispose()
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithRequiredParameters_ReturnsCorrectBurnTokensBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            BigInteger amount = new BigInteger(1000);

            _builder.WithOrigin(new Url(origin));
            _builder.WithAmount(amount);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as BurnTokens;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("burnTokens", body.Type);
            Assert.Equal(amount, body.Amount);
        }
        
        [Fact]
        public void BuildTransactionBody_WithLongAmount_ReturnsCorrectBurnTokensBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            long amount = 1000;

            _builder.WithOrigin(new Url(origin));
            _builder.WithAmount(amount);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as BurnTokens;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("burnTokens", body.Type);
            Assert.Equal(new BigInteger(amount), body.Amount);
        }
        
        [Fact]
        public void BuildTransactionBody_WithStringAmount_ReturnsCorrectBurnTokensBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string amount = "1000";

            _builder.WithOrigin(new Url(origin));
            _builder.WithAmount(amount);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as BurnTokens;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("burnTokens", body.Type);
            Assert.Equal(BigInteger.Parse(amount), body.Amount);
        }

        [Fact]
        public async Task Validate_WithoutOrigin_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithAmount(1000);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Origin must be set", exception.Message);
        }

        [Fact]
        public async Task Validate_WithoutAmount_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Amount must be greater than zero", exception.Message);
        }

        [Fact]
        public async Task Validate_WithZeroAmount_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithAmount(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Amount must be greater than zero", exception.Message);
        }

        [Fact]
        public void WithAmount_BigIntegerNegative_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                _builder.WithAmount(BigInteger.Parse("-1")));
        }

        [Fact]
        public void WithAmount_LongNegative_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                _builder.WithAmount(-1L));
        }

        [Fact]
        public void WithAmount_StringNegative_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                _builder.WithAmount("-1"));
        }

        [Fact]
        public void WithAmount_EmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _builder.WithAmount(string.Empty));
        }

        [Fact]
        public void WithAmount_InvalidString_ThrowsFormatException()
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => 
                _builder.WithAmount("not-a-number"));
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithAmount(1000);

            // Act
            await _builder.ExecuteAsync();

            // Assert
            _apiClientMock.Verify(c => c.ExecuteAsync(It.IsAny<BurnTokens>()), Times.Once);
        }
    }
} 