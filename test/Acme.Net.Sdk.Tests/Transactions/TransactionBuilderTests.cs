using System;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Rpc.Models;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Moq;
using System.Text.Json;
using Xunit;

namespace Acme.Net.Sdk.Tests.Transactions
{
    public class TransactionBuilderTests : IDisposable
    {
        private readonly string? _originalAccApi;
        private readonly TestApiClient _apiClient;
        private readonly TestTransactionBuilder _builder;
        private readonly TestAsyncRPCClient _rpcClient;

        public TransactionBuilderTests()
        {
            _originalAccApi = Environment.GetEnvironmentVariable("ACC_API");
            Environment.SetEnvironmentVariable("ACC_API", "http://test-endpoint.com");

            _rpcClient = new TestAsyncRPCClient();
            _apiClient = new TestApiClient(_rpcClient);
            
            _builder = new TestTransactionBuilder(_apiClient);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ACC_API", _originalAccApi);
        }

        [Fact]
        public void BuildTransactionBody_ReturnsCorrectBody()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://example.acme"));

            // Act
            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.NotNull(body);
            Assert.IsType<TestTransaction>(body);
            Assert.Equal("testTransaction", ((TestTransaction)body).Type);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_CallsApiClientExecuteAsync()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://example.acme"));
            
            var txResponse = new TxResponse
            {
                TxId = "0123456789abcdef"
            };

            _apiClient.SetupExecuteAsync(txResponse);

            // Act
            var result = await _builder.ExecuteAsync();

            // Assert
            Assert.Equal(txResponse, result);
            _apiClient.VerifyExecuteAsyncCalled();
        }

        [Fact]
        public async Task ExecuteSignedAsync_WithValidParameters_CallsRpcClientSendTxAsync()
        {
            // Arrange
            var origin = new Url("acc://example.acme");
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            
            // Setup signer with all required properties
            var signer = new Signer()
                .WithUrl(origin)
                .WithKeyPair(keyPair)
                .WithType(SignatureType.ED25519)
                .WithVersion(1)
                .WithTimeStamp(123456789);
            
            // Setup builder
            _builder.WithOrigin(origin)
                   .WithSigner(signer);
            
            // Setup expected result from the RPC client
            var txResponse = new TxResponse
            {
                TxId = "0123456789abcdef"
            };
            
            // Create JsonElement to represent the response
            var jsonString = JsonSerializer.Serialize(txResponse);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;
            
            // Setup the RPC client mock to return the test result
            _rpcClient.EnvelopeResponse = jsonElement;

            // Act
            var result = await _builder.ExecuteSignedAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.TxId);
        }
        
        [Fact]
        public void ExecuteSignedAsync_WithoutSigner_ThrowsInvalidOperationException()
        {
            // Arrange
            _builder.WithOrigin(new Url("acc://example.acme"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _builder.ExecuteSignedAsync());
        }
    }

    // Test AsyncRPCClient that doesn't make actual network calls
    public class TestAsyncRPCClient : AsyncRPCClient
    {
        private readonly Uri _testEndpoint = new Uri("http://test-endpoint.com");
        
        public TestAsyncRPCClient() 
            : base(new Uri("http://test-endpoint.com"))
        {
        }

        public bool EnvelopeSendCalled { get; private set; }
        public object? EnvelopeResponse { get; set; }

        // Completely override without calling base functionality to avoid network calls
        public override Task<object?> SendTxAsync(Envelope envelope)
        {
            EnvelopeSendCalled = true;
            
            // Create a fake response based on the EnvelopeResponse property
            if (EnvelopeResponse is JsonElement element)
            {
                // Parse the JSON string into a TxResponse
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = element.GetRawText();
                return Task.FromResult<object?>(JsonSerializer.Deserialize<TxResponse>(json, options));
            }
            
            return Task.FromResult(EnvelopeResponse);
        }
    }

    // Test-specific ApiClient to allow mocking
    public class TestApiClient : ApiClient
    {
        private readonly Mock<ApiClient> _mock = new Mock<ApiClient>();

        public TestApiClient(AsyncRPCClient rpcClient) 
        {
            RpcClient = rpcClient;
        }

        public new AsyncRPCClient RpcClient { get; }

        public void SetupExecuteAsync(TxResponse response)
        {
            _mock.Setup(client => client.ExecuteAsync(It.IsAny<ITransactionBody>()))
                .ReturnsAsync(response);
        }

        public void VerifyExecuteAsyncCalled()
        {
            _mock.Verify(client => client.ExecuteAsync(It.IsAny<ITransactionBody>()), Times.Once);
        }

        public override Task<TxResponse> ExecuteAsync(ITransactionBody body)
        {
            return _mock.Object.ExecuteAsync(body);
        }
    }

    // Simple test transaction builder for testing
    public class TestTransactionBuilder : TransactionBuilder
    {
        public TestTransactionBuilder(ApiClient client) : base(client)
        {
        }

        protected override ITransactionBody BuildTransactionBody()
        {
            return new TestTransaction();
        }
    }

    // Simple test transaction body implementation
    public class TestTransaction : ITransactionBody
    {
        public string Type => "testTransaction";

        public byte[] MarshalBinary()
        {
            // Simple implementation for testing
            return new byte[] { 1, 2, 3, 4, 5 };
        }
    }

    // Extension method to expose protected BuildTransactionBody for testing
    public static class TestTransactionBuilderExtensions
    {
        public static ITransactionBody BuildTransactionBodyForTest(this TestTransactionBuilder builder)
        {
            // Use reflection to call the protected method
            var methodInfo = typeof(TestTransactionBuilder).GetMethod("BuildTransactionBody", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (ITransactionBody)methodInfo.Invoke(builder, null);
        }
    }
} 