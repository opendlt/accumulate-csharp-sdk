using System;
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
    public class CreateTokenBuilderTests
    {
        private readonly TestApiClient _apiClient;
        private readonly CreateTokenBuilder _builder;
        private readonly string _defaultTokenUrl = "acc://acme.acme/tokens/mytoken";
        private readonly string _defaultTokenSymbol = "TKN";

        public CreateTokenBuilderTests()
        {
            var rpcClient = new TestAsyncRPCClient();
            _apiClient = new TestApiClient(rpcClient);
            _builder = new CreateTokenBuilder(_apiClient);
            
            // Set origin for all tests to avoid validation errors
            ((TransactionBuilder)_builder).WithOrigin("acc://acme.acme");
            
            // Set required fields for all tests
            _builder.WithUrl(_defaultTokenUrl);
            _builder.WithSymbol(_defaultTokenSymbol);
        }

        [Fact]
        public void WithUrl_SetsUrl()
        {
            // Arrange
            var url = new Url("acc://acme.acme/tokens/mytoken2");

            // Act
            _builder.WithUrl(url);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<CreateToken>(body);
            Assert.Equal(url, ((CreateToken)body).Url);
        }

        [Fact]
        public void WithUrl_String_SetsUrl()
        {
            // Arrange
            var urlString = "acc://acme.acme/tokens/mytoken2";

            // Act
            _builder.WithUrl(urlString);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<CreateToken>(body);
            Assert.Equal(urlString, ((CreateToken)body).Url?.ToString());
        }

        [Fact]
        public void WithSymbol_SetsSymbol()
        {
            // Arrange
            var symbol = "TKN2";

            // Act
            _builder.WithSymbol(symbol);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<CreateToken>(body);
            Assert.Equal(symbol, ((CreateToken)body).Symbol);
        }

        [Fact]
        public void WithPrecision_SetsPrecision()
        {
            // Arrange
            var precision = 10;

            // Act
            _builder.WithPrecision(precision);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<CreateToken>(body);
            Assert.Equal(precision, ((CreateToken)body).Precision);
        }

        [Fact]
        public void WithSupplyLimit_SetsSupplyLimit()
        {
            // Arrange
            ulong supplyLimit = 1000000;

            // Act
            _builder.WithSupplyLimit(supplyLimit);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<CreateToken>(body);
            Assert.Equal(supplyLimit, ((CreateToken)body).SupplyLimit);
        }

        [Fact]
        public void WithProperties_SetsProperties()
        {
            // Arrange
            // Properties should be a URL, not JSON (per test vectors)
            var properties = "acc://token-properties.acme";

            // Act
            _builder.WithProperties(properties);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<CreateToken>(body);
            Assert.NotNull(((CreateToken)body).Properties);
            Assert.Equal(properties, ((CreateToken)body).Properties.ToString());
        }

        [Fact]
        public void Validate_WithoutRequiredFields_ThrowsException()
        {
            // Arrange - create a new builder without origin
            var newBuilder = new CreateTokenBuilder(_apiClient);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => newBuilder.BuildTransactionBodyForTest());
            Assert.Equal("Origin must be set", exception.Message);
        }

        [Fact]
        public void BuildTransactionBody_WithRequiredFields_ReturnsValidBody()
        {
            // Act
            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.IsType<CreateToken>(body);
            var createToken = (CreateToken)body;
            Assert.Equal(_defaultTokenUrl, createToken.Url?.ToString());
            Assert.Equal(_defaultTokenSymbol, createToken.Symbol);
            Assert.Equal(8, createToken.Precision); // Default value
        }

        [Fact]
        public async Task ExecuteAsync_CallsApiClientExecuteAsync()
        {
            // Arrange
            var response = new TxResponse { TxId = "tx123" };
            _apiClient.SetupExecuteAsync(response);

            // Act
            var result = await _builder.ExecuteAsync();

            // Assert
            Assert.Equal(response, result);
            _apiClient.VerifyExecuteAsyncCalled();
        }
    }
}

// Extension method to expose protected BuildTransactionBody for testing
public static class CreateTokenBuilderExtensions
{
    public static ITransactionBody BuildTransactionBodyForTest(this CreateTokenBuilder builder)
    {
        try
        {
            // Use reflection to invoke the protected method
            var method = builder.GetType().BaseType?.GetMethod("BuildTransactionBody", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (ITransactionBody)method?.Invoke(builder, null)!;
        }
        catch (System.Reflection.TargetInvocationException tie)
        {
            // Unwrap the inner exception if it's a TargetInvocationException
            if (tie.InnerException != null)
                throw tie.InnerException;
            throw;
        }
    }
} 