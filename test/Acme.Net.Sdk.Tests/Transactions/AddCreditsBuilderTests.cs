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
    public class AddCreditsBuilderTests
    {
        private readonly TestApiClient _apiClient;
        private readonly AddCreditsBuilder _builder;
        private readonly string _defaultRecipientUrl = "acc://acme.acme/account";
        private readonly long _defaultAmount = 1000;

        public AddCreditsBuilderTests()
        {
            var rpcClient = new TestAsyncRPCClient();
            _apiClient = new TestApiClient(rpcClient);
            _builder = new AddCreditsBuilder(_apiClient);
            
            // Set origin for all tests to avoid validation errors
            ((TransactionBuilder)_builder).WithOrigin("acc://acme.acme");
            
            // Set required fields for all tests
            _builder.WithRecipient(_defaultRecipientUrl);
            _builder.WithAmount(_defaultAmount);
        }

        [Fact]
        public void WithRecipient_SetsRecipient()
        {
            // Arrange
            var recipient = new Url("acc://acme.acme/account2");

            // Act
            _builder.WithRecipient(recipient);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<AddCredits>(body);
            Assert.Equal(recipient, ((AddCredits)body).Recipient);
        }

        [Fact]
        public void WithRecipient_String_SetsRecipient()
        {
            // Arrange
            var recipientString = "acc://acme.acme/account2";

            // Act
            _builder.WithRecipient(recipientString);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<AddCredits>(body);
            Assert.Equal(recipientString, ((AddCredits)body).Recipient?.ToString());
        }

        [Fact]
        public void WithAmount_BigInteger_SetsAmount()
        {
            // Arrange
            var amount = new BigInteger(2000);

            // Act
            _builder.WithAmount(amount);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<AddCredits>(body);
            Assert.Equal(amount, ((AddCredits)body).Amount);
        }

        [Fact]
        public void WithAmount_Long_SetsAmount()
        {
            // Arrange
            long amount = 2000;

            // Act
            _builder.WithAmount(amount);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<AddCredits>(body);
            Assert.Equal(new BigInteger(amount), ((AddCredits)body).Amount);
        }

        [Fact]
        public void WithOracle_SetsOracle()
        {
            // Arrange
            var oracle = "signaturedata";

            // Act
            _builder.WithOracle(oracle);

            // Assert - verify through BuildTransactionBody
            var body = _builder.BuildTransactionBodyForTest();
            Assert.IsType<AddCredits>(body);
            Assert.Equal(oracle, ((AddCredits)body).Oracle);
        }

        [Fact]
        public void Validate_WithoutRequiredFields_ThrowsException()
        {
            // Arrange - create a new builder without origin
            var newBuilder = new AddCreditsBuilder(_apiClient);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => newBuilder.BuildTransactionBodyForTest());
            Assert.Equal("Origin must be set", exception.Message);
        }

        [Fact]
        public void Validate_WithZeroAmount_ThrowsException()
        {
            // Arrange - create a new builder with origin and recipient but zero amount
            var newBuilder = new AddCreditsBuilder(_apiClient);
            ((TransactionBuilder)newBuilder).WithOrigin("acc://acme.acme");
            newBuilder.WithRecipient(_defaultRecipientUrl).WithAmount(0);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => newBuilder.BuildTransactionBodyForTest());
            Assert.Equal("Amount must be greater than zero", exception.Message);
        }

        [Fact]
        public void BuildTransactionBody_WithRequiredFields_ReturnsValidBody()
        {
            // Act
            var body = _builder.BuildTransactionBodyForTest();

            // Assert
            Assert.IsType<AddCredits>(body);
            var addCredits = (AddCredits)body;
            Assert.Equal(_defaultRecipientUrl, addCredits.Recipient?.ToString());
            Assert.Equal(new BigInteger(_defaultAmount), addCredits.Amount);
            Assert.Null(addCredits.Oracle);
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
public static class AddCreditsBuilderExtensions
{
    public static ITransactionBody BuildTransactionBodyForTest(this AddCreditsBuilder builder)
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