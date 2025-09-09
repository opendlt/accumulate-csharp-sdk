#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CreateKeyPageBuilderTests : IDisposable
    {
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly CreateKeyPageBuilder _builder;
        private readonly string? _originalAccApi;

        public CreateKeyPageBuilderTests()
        {
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");

            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _builder = new CreateKeyPageBuilder(_apiClientMock.Object);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithSingleKey_ReturnsCorrectCreateKeyPageBody()
        {
            // Arrange
            Url keyPageUrl = new Url("acc://keybook.acme/page");
            byte[] key = new byte[] { 0x01, 0x02, 0x03 };

            // Act
            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithKeyPageUrl(keyPageUrl)
                .AddKey(key);

            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<CreateKeyPage>(body);
            var createKeyPage = (CreateKeyPage)body;
            Assert.Equal("createKeyPage", createKeyPage.Type);
            Assert.Equal(keyPageUrl, createKeyPage.Url);
            Assert.Single(createKeyPage.Keys);
            Assert.Equal(key, createKeyPage.Keys.First().KeyHash);
        }

        [Fact]
        public void BuildTransactionBody_WithMultipleKeys_ReturnsCorrectCreateKeyPageBody()
        {
            // Arrange
            Url keyPageUrl = new Url("acc://keybook.acme/page");
            byte[] key1 = new byte[] { 0x01, 0x02, 0x03 };
            byte[] key2 = new byte[] { 0x04, 0x05, 0x06 };
            byte[] key3 = new byte[] { 0x07, 0x08, 0x09 };

            // Act
            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithKeyPageUrl(keyPageUrl)
                .AddKey(key1)
                .AddKey(key2)
                .AddKey(key3);

            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<CreateKeyPage>(body);
            var createKeyPage = (CreateKeyPage)body;
            Assert.Equal("createKeyPage", createKeyPage.Type);
            Assert.Equal(keyPageUrl, createKeyPage.Url);
            Assert.Equal(3, createKeyPage.Keys.Count);
            Assert.Equal(key1, createKeyPage.Keys[0].KeyHash);
            Assert.Equal(key2, createKeyPage.Keys[1].KeyHash);
            Assert.Equal(key3, createKeyPage.Keys[2].KeyHash);
        }

        [Fact]
        public void BuildTransactionBody_WithKeysList_ReturnsCorrectCreateKeyPageBody()
        {
            // Arrange
            Url keyPageUrl = new Url("acc://keybook.acme/page");
            var keys = new List<byte[]>
            {
                new byte[] { 0x01, 0x02, 0x03 },
                new byte[] { 0x04, 0x05, 0x06 }
            };

            // Act
            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithKeyPageUrl(keyPageUrl)
                .WithKeys(keys);

            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<CreateKeyPage>(body);
            var createKeyPage = (CreateKeyPage)body;
            Assert.Equal("createKeyPage", createKeyPage.Type);
            Assert.Equal(keyPageUrl, createKeyPage.Url);
            Assert.Equal(2, createKeyPage.Keys.Count);
            Assert.Equal(keys[0], createKeyPage.Keys[0].KeyHash);
            Assert.Equal(keys[1], createKeyPage.Keys[1].KeyHash);
        }

        [Fact]
        public void BuildTransactionBody_WithKeysArray_ReturnsCorrectCreateKeyPageBody()
        {
            // Arrange
            Url keyPageUrl = new Url("acc://keybook.acme/page");
            var keys = new byte[][]
            {
                new byte[] { 0x01, 0x02, 0x03 },
                new byte[] { 0x04, 0x05, 0x06 }
            };

            // Act
            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithKeyPageUrl(keyPageUrl)
                .WithKeys(keys);

            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<CreateKeyPage>(body);
            var createKeyPage = (CreateKeyPage)body;
            Assert.Equal("createKeyPage", createKeyPage.Type);
            Assert.Equal(keyPageUrl, createKeyPage.Url);
            Assert.Equal(2, createKeyPage.Keys.Count);
            Assert.Equal(keys[0], createKeyPage.Keys[0].KeyHash);
            Assert.Equal(keys[1], createKeyPage.Keys[1].KeyHash);
        }

        [Fact]
        public void Validate_WithoutKeyPageUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            ((TransactionBuilder)_builder).WithOrigin(new Url("acc://example.acme"));
            _builder.AddKey(new byte[] { 0x01, 0x02, 0x03 });

            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => _builder.BuildTransactionBodyForTest());
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("Key page URL must be set", exception.InnerException?.Message);
        }

        [Fact]
        public void Validate_WithoutKeys_ThrowsInvalidOperationException()
        {
            // Arrange
            ((TransactionBuilder)_builder).WithOrigin(new Url("acc://example.acme"));
            _builder.WithKeyPageUrl(new Url("acc://keypage.acme"));

            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => _builder.BuildTransactionBodyForTest());
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("At least one key must be specified", exception.InnerException?.Message);
        }

        [Fact]
        public void WithKeyPageUrl_NullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyPageUrl((Url)null));
        }

        [Fact]
        public void WithKeyPageUrl_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyPageUrl((string)null));
        }

        [Fact]
        public void WithKeyPageUrl_EmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyPageUrl(string.Empty));
        }

        [Fact]
        public void AddKey_NullKey_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.AddKey(null));
        }

        [Fact]
        public void WithKeys_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeys((List<byte[]>)null));
        }

        [Fact]
        public void WithKeys_EmptyList_ThrowsArgumentException()
        {
            // Arrange
            var emptyList = new List<byte[]>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.WithKeys(emptyList));
        }

        [Fact]
        public void WithKeys_NullArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeys((byte[][])null));
        }

        [Fact]
        public void WithKeys_EmptyArray_ThrowsArgumentException()
        {
            // Arrange
            var emptyArray = new byte[0][];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.WithKeys(emptyArray));
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            Url keyPageUrl = new Url("acc://keybook.acme/page");
            byte[] key = new byte[] { 0x01, 0x02, 0x03 };

            ((TransactionBuilder)_builder)
                .WithOrigin(new Url("acc://example.acme"));

            _builder
                .WithKeyPageUrl(keyPageUrl)
                .AddKey(key);
            
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
    public static class CreateKeyPageBuilderExtensions
    {
        public static ITransactionBody BuildTransactionBodyForTest(this CreateKeyPageBuilder builder)
        {
            // Use reflection to call the protected method
            var methodInfo = typeof(CreateKeyPageBuilder).GetMethod("BuildTransactionBody", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (ITransactionBody)methodInfo.Invoke(builder, null);
        }
    }
} 