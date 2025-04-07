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
using System.Text;

namespace Acme.Net.Sdk.Tests.Transactions
{
    public class WriteDataToBuilderTests : IDisposable
    {
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly WriteDataToBuilder _builder;
        private readonly string? _originalAccApi;

        public WriteDataToBuilderTests()
        {
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");

            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _builder = new WriteDataToBuilder(_apiClientMock.Object);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithValidParameters_ReturnsCorrectWriteDataToBody()
        {
            // Arrange
            Url recipient = new Url("acc://recipient.acme");
            byte[] data = new byte[] { 0x01, 0x02, 0x03 };
            string format = "json";
            string entryHash = "0123456789abcdef";

            // Act
            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithRecipient(recipient)
                .WithData(data)
                .WithFormat(format)
                .WithEntryHash(entryHash);

            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<WriteDataTo>(body);
            var writeDataTo = (WriteDataTo)body;
            Assert.Equal("writeDataTo", writeDataTo.Type);
            Assert.Equal(recipient, writeDataTo.Recipient);
            Assert.Equal(data, writeDataTo.Data);
            Assert.Equal(format, writeDataTo.Format);
            Assert.Equal(entryHash, writeDataTo.EntryHash);
        }

        [Fact]
        public void Validate_WithoutRecipient_ThrowsInvalidOperationException()
        {
            // Arrange
            ((TransactionBuilder)_builder).WithOrigin(new Url("acc://example.acme"));
            _builder.WithData(Encoding.UTF8.GetBytes("Test data"));

            // Act & Assert
            var ex = Assert.Throws<TargetInvocationException>(() => _builder.BuildTransactionBodyForTest());
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Contains("Recipient URL must be set", ex.InnerException.Message);
        }

        [Fact]
        public void Validate_WithoutData_ThrowsInvalidOperationException()
        {
            // Arrange
            ((TransactionBuilder)_builder).WithOrigin(new Url("acc://example.acme"));
            _builder.WithRecipient(new Url("acc://recipient.acme"));

            // Act & Assert
            var ex = Assert.Throws<TargetInvocationException>(() => _builder.BuildTransactionBodyForTest());
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Contains("Data must be set", ex.InnerException.Message);
        }

        [Fact]
        public void WithRecipient_NullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithRecipient((Url)null));
        }

        [Fact]
        public void WithRecipient_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithRecipient((string)null));
        }

        [Fact]
        public void WithRecipient_EmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithRecipient(string.Empty));
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
        public void WithEntryHash_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithEntryHash(null));
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            Url recipient = new Url("acc://recipient.acme");
            byte[] data = new byte[] { 0x01, 0x02, 0x03 };

            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithRecipient(recipient)
                .WithData(data);
            
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
    public static class WriteDataToBuilderExtensions
    {
        public static ITransactionBody BuildTransactionBodyForTest(this WriteDataToBuilder builder)
        {
            // First trigger validation (which is called before BuildTransactionBody in real code)
            var validateMethod = typeof(TransactionBuilder).GetMethod("Validate", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            validateMethod.Invoke(builder, null);
            
            // Then call the build method
            var methodInfo = typeof(WriteDataToBuilder).GetMethod("BuildTransactionBody", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (ITransactionBody)methodInfo.Invoke(builder, null);
        }
    }
} 