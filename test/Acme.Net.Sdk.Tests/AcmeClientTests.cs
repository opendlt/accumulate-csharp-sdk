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
        private readonly Mock<AsyncRPCClient> _rpcClientMock;
        private readonly AcmeClient _client;
        
        public AcmeClientTests()
        {
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");

            _rpcClientMock = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            _client = new AcmeClient(_rpcClientMock.Object);
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

        [Fact]
        public void CreateTokenAccountBuilder_ReturnsCreateTokenAccountBuilder()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateTokenAccountBuilder();

            // Assert
            Assert.NotNull(builder);
            Assert.IsType<CreateTokenAccountBuilder>(builder);
        }

        [Fact]
        public void CreateTokenAccountBuilder_BuilderUsesAccountsClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateTokenAccountBuilder();
            
            // Get protected Client property via reflection
            var clientProperty = builder.GetType().BaseType?.GetProperty("Client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var apiClient = clientProperty?.GetValue(builder);

            // Assert
            Assert.NotNull(apiClient);
            Assert.IsType<AccountsClient>(apiClient);
        }

        [Fact]
        public void CreateBurnTokensBuilder_ReturnsBurnTokensBuilder()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateBurnTokensBuilder();

            // Assert
            Assert.NotNull(builder);
            Assert.IsType<BurnTokensBuilder>(builder);
        }

        [Fact]
        public void CreateBurnTokensBuilder_BuilderUsesTokensClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateBurnTokensBuilder();
            
            // Get protected Client property via reflection
            var clientProperty = builder.GetType().BaseType?.GetProperty("Client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var apiClient = clientProperty?.GetValue(builder);

            // Assert
            Assert.NotNull(apiClient);
            Assert.IsType<TokensClient>(apiClient);
        }

        [Fact]
        public void CreateKeyBookBuilder_ReturnsCreateKeyBookBuilder()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateKeyBookBuilder();

            // Assert
            Assert.NotNull(builder);
            Assert.IsType<CreateKeyBookBuilder>(builder);
        }

        [Fact]
        public void CreateKeyBookBuilder_BuilderUsesAccountsClient()
        {
            // Arrange
            var mockRpcClient = new Mock<AsyncRPCClient>(new Uri("http://test-endpoint.com"));
            var client = new AcmeClient(mockRpcClient.Object);

            // Act
            var builder = client.CreateKeyBookBuilder();
            
            // Get protected Client property via reflection
            var clientProperty = builder.GetType().BaseType?.GetProperty("Client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var apiClient = clientProperty?.GetValue(builder);

            // Assert
            Assert.NotNull(apiClient);
            Assert.IsType<AccountsClient>(apiClient);
        }
    }
} 