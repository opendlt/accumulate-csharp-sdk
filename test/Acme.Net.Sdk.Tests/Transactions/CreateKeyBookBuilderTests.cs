#nullable enable annotations

using System;
using System.Collections.Generic;
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
    public class CreateKeyBookBuilderTests : IDisposable
    {
        private readonly CreateKeyBookBuilder _builder;
        private readonly Mock<ApiClient> _apiClientMock;
        private readonly string? _originalAccApi;
        
        public CreateKeyBookBuilderTests()
        {
            // Save original environment variable value
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            // Set test environment variable
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");
            
            // Set up mock AccountsClient using a specific endpoint to avoid environment variable issues
            _apiClientMock = new Mock<ApiClient>(new Uri("http://test-endpoint.com"));
            _apiClientMock.Setup(c => c.ExecuteAsync(It.IsAny<CreateKeyBook>()))
                .ReturnsAsync(new TxResponse());
                
            _builder = new CreateKeyBookBuilder(_apiClientMock.Object);
        }
        
        public void Dispose()
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_WithRequiredParameters_ReturnsCorrectCreateKeyBookBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string keyBookUrl = "acc://keybook.acme";
            string keyPageUrl = "acc://keypage.acme";

            _builder.WithOrigin(new Url(origin));
            _builder.WithKeyBookUrl(new Url(keyBookUrl));
            _builder.AddKeyPage(new Url(keyPageUrl));

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as CreateKeyBook;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("createKeyBook", body.Type);
            Assert.Equal(keyBookUrl, body.Url.ToString());
            Assert.Single(body.Pages);
            Assert.Equal(keyPageUrl, body.Pages[0].ToString());
        }
        
        [Fact]
        public void BuildTransactionBody_WithMultipleKeyPages_ReturnsCorrectCreateKeyBookBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string keyBookUrl = "acc://keybook.acme";
            string keyPageUrl1 = "acc://keypage1.acme";
            string keyPageUrl2 = "acc://keypage2.acme";

            _builder.WithOrigin(new Url(origin));
            _builder.WithKeyBookUrl(new Url(keyBookUrl));
            _builder.AddKeyPage(new Url(keyPageUrl1));
            _builder.AddKeyPage(new Url(keyPageUrl2));

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as CreateKeyBook;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("createKeyBook", body.Type);
            Assert.Equal(keyBookUrl, body.Url.ToString());
            Assert.Equal(2, body.Pages.Count);
            Assert.Equal(keyPageUrl1, body.Pages[0].ToString());
            Assert.Equal(keyPageUrl2, body.Pages[1].ToString());
        }
        
        [Fact]
        public void BuildTransactionBody_WithKeyPagesArray_ReturnsCorrectCreateKeyBookBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string keyBookUrl = "acc://keybook.acme";
            string keyPageUrl1 = "acc://keypage1.acme";
            string keyPageUrl2 = "acc://keypage2.acme";

            var keyPageUrls = new Url[]
            {
                new Url(keyPageUrl1),
                new Url(keyPageUrl2)
            };

            _builder.WithOrigin(new Url(origin));
            _builder.WithKeyBookUrl(new Url(keyBookUrl));
            _builder.WithKeyPages(keyPageUrls);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as CreateKeyBook;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("createKeyBook", body.Type);
            Assert.Equal(keyBookUrl, body.Url.ToString());
            Assert.Equal(2, body.Pages.Count);
            Assert.Equal(keyPageUrl1, body.Pages[0].ToString());
            Assert.Equal(keyPageUrl2, body.Pages[1].ToString());
        }
        
        [Fact]
        public void BuildTransactionBody_WithKeyPagesList_ReturnsCorrectCreateKeyBookBody()
        {
            // Arrange
            string origin = "acc://origin.acme";
            string keyBookUrl = "acc://keybook.acme";
            string keyPageUrl1 = "acc://keypage1.acme";
            string keyPageUrl2 = "acc://keypage2.acme";

            var keyPageUrls = new List<Url>
            {
                new Url(keyPageUrl1),
                new Url(keyPageUrl2)
            };

            _builder.WithOrigin(new Url(origin));
            _builder.WithKeyBookUrl(new Url(keyBookUrl));
            _builder.WithKeyPages(keyPageUrls);

            // Act
            var body = _builder.GetType()
                .GetMethod("BuildTransactionBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_builder, null) as CreateKeyBook;

            // Assert
            Assert.NotNull(body);
            Assert.Equal("createKeyBook", body.Type);
            Assert.Equal(keyBookUrl, body.Url.ToString());
            Assert.Equal(2, body.Pages.Count);
            Assert.Equal(keyPageUrl1, body.Pages[0].ToString());
            Assert.Equal(keyPageUrl2, body.Pages[1].ToString());
        }

        [Fact]
        public async Task Validate_WithoutOrigin_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithKeyBookUrl(new Url("acc://keybook.acme"));
            _builder.AddKeyPage(new Url("acc://keypage.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Origin must be set", exception.Message);
        }

        [Fact]
        public async Task Validate_WithoutKeyBookUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.AddKeyPage(new Url("acc://keypage.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("Key book URL must be set", exception.Message);
        }

        [Fact]
        public async Task Validate_WithoutKeyPages_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithKeyBookUrl(new Url("acc://keybook.acme"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _builder.ExecuteAsync());
            Assert.Contains("At least one key page URL must be specified", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://origin.acme"));
            _builder.WithKeyBookUrl(new Url("acc://keybook.acme"));
            _builder.AddKeyPage(new Url("acc://keypage.acme"));

            // Act
            await _builder.ExecuteAsync();

            // Assert
            _apiClientMock.Verify(c => c.ExecuteAsync(It.IsAny<CreateKeyBook>()), Times.Once);
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

        [Fact]
        public void AddKeyPage_WithNullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.AddKeyPage((Url)null));
        }

        [Fact]
        public void AddKeyPage_WithEmptyStringUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.AddKeyPage(string.Empty));
        }

        [Fact]
        public void WithKeyPages_WithNullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyPages((List<Url>)null));
        }

        [Fact]
        public void WithKeyPages_WithNullArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.WithKeyPages((Url[])null));
        }
    }
} 