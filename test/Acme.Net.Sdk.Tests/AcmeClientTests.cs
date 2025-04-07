using System;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Transactions;
using Moq;
using Xunit;

namespace Acme.Net.Sdk.Tests
{
    public class AcmeClientTests : IDisposable
    {
        private readonly string _originalAccApi;
        
        public AcmeClientTests()
        {
            // Save original environment variable value
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            // Set test environment variable
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");
        }
        
        public void Dispose()
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }
        
        [Fact]
        public void Constructor_WithNoParameters_CreatesInstanceWithDefaultRpcClient()
        {
            // Act
            var client = new AcmeClient();

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void Constructor_WithEndpointUri_CreatesInstanceWithCustomEndpoint()
        {
            // Arrange
            var endpoint = new Uri("http://test-endpoint.com");

            // Act
            var client = new AcmeClient(endpoint);

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void Constructor_WithEndpointString_CreatesInstanceWithCustomEndpoint()
        {
            // Arrange
            var endpoint = "http://test-endpoint.com";

            // Act
            var client = new AcmeClient(endpoint);

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void Constructor_WithRpcClient_CreatesInstanceWithCustomRpcClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));

            // Act
            var client = new AcmeClient(mockRpcClient.Object);

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void Constructor_WithNullRpcClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AcmeClient((AsyncRPCClient)null));
        }

        [Fact]
        public void Accounts_ReturnsAccountsClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var accountsClient = client.Accounts();

            // Assert
            Assert.NotNull(accountsClient);
            Assert.IsType<AccountsClient>(accountsClient);
        }

        [Fact]
        public void Tokens_ReturnsTokensClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var tokensClient = client.Tokens();

            // Assert
            Assert.NotNull(tokensClient);
            Assert.IsType<TokensClient>(tokensClient);
        }

        [Fact]
        public void CreateSendTokensBuilder_ReturnsSendTokensBuilder()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateSendTokensBuilder();

            // Assert
            Assert.NotNull(builder);
            Assert.IsType<SendTokensBuilder>(builder);
        }

        [Fact]
        public void CreateIssueTokensBuilder_ReturnsIssueTokensBuilder()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateIssueTokensBuilder();

            // Assert
            Assert.NotNull(builder);
            Assert.IsType<IssueTokensBuilder>(builder);
        }

        [Fact]
        public void CreateSendTokensBuilder_BuilderUsesTokensClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateSendTokensBuilder();
            
            // Get protected Client property via reflection
            var clientProperty = builder.GetType().BaseType?.GetProperty("Client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var apiClient = clientProperty?.GetValue(builder);

            // Assert
            Assert.NotNull(apiClient);
            Assert.IsType<TokensClient>(apiClient);
        }

        [Fact]
        public void CreateIssueTokensBuilder_BuilderUsesTokensClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateIssueTokensBuilder();
            
            // Get protected Client property via reflection
            var clientProperty = builder.GetType().BaseType?.GetProperty("Client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var apiClient = clientProperty?.GetValue(builder);

            // Assert
            Assert.NotNull(apiClient);
            Assert.IsType<TokensClient>(apiClient);
        }
    }
} 