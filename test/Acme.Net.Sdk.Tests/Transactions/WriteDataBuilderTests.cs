using System;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Transactions;
using Moq;
using Xunit;
using System.Reflection;

namespace Acme.Net.Sdk.Tests.Transactions
{
    public class WriteDataBuilderTests : IDisposable
    {
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly WriteDataBuilder _builder;
        private readonly string _originalAccApi;

        public WriteDataBuilderTests()
        {
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");

            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _builder = new WriteDataBuilder(_apiClientMock.Object);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithValidParameters_ReturnsCorrectWriteDataBody()
        {
            // Arrange
            byte[] data = new byte[] { 0x01, 0x02, 0x03 };
            string format = "json";
            string entryHash = "0123456789abcdef";

            // Act
            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithData(data)
                .WithFormat(format)
                .WithEntryHash(entryHash);

            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<WriteData>(body);
            var writeData = (WriteData)body;
            Assert.Equal("writeData", writeData.Type);
            Assert.Equal(data, writeData.Data);
            Assert.Equal(format, writeData.Format);
            Assert.Equal(entryHash, writeData.EntryHash);
        }

        [Fact]
        public void Validate_WithoutData_ThrowsInvalidOperationException()
        {
            // Arrange
            ((TransactionBuilder)_builder).WithOrigin(new Url("acc://example.acme"));

            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => _builder.BuildTransactionBodyForTest());
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("Data must be set", exception.InnerException?.Message);
        }

        [Fact]
        public void WithData_NullByteArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithData((byte[])null));
        }

        [Fact]
        public void WithData_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithData((string)null));
        }

        [Fact]
        public void WithData_EmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithData(string.Empty));
        }

        [Fact]
        public void WithFormat_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithFormat(null));
        }

        [Fact]
        public void WithFormat_EmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithFormat(string.Empty));
        }

        [Fact]
        public void WithEntryHash_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithEntryHash(null));
        }

        [Fact]
        public void WithEntryHash_EmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithEntryHash(string.Empty));
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            byte[] data = new byte[] { 0x01, 0x02, 0x03 };

            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder.WithData(data);
            
            var txResponse = new TxResponse
            {
                TxId = "0123456789abcdef"
            };

            _apiClientMock.Setup(client => client.ExecuteAsync(It.IsAny<ITransactionBody>()))
                .ReturnsAsync(txResponse);

            // Act
            var result = await _builder.ExecuteAsync();

            // Assert
            Assert.Equal(txResponse, result);
            _apiClientMock.Verify(client => client.ExecuteAsync(It.IsAny<ITransactionBody>()), Times.Once);
        }
    }

    // Extension method to expose protected BuildTransactionBody for testing
    public static class WriteDataBuilderExtensions
    {
        public static ITransactionBody BuildTransactionBodyForTest(this WriteDataBuilder builder)
        {
            // Use reflection to call the protected method
            var methodInfo = typeof(WriteDataBuilder).GetMethod("BuildTransactionBody", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (ITransactionBody)methodInfo.Invoke(builder, null);
        }
    }
} 