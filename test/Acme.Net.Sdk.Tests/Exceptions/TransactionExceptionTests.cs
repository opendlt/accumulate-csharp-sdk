using System;
using Xunit;
using Acme.Net.Sdk.Exceptions;

namespace Acme.Net.Sdk.Tests.Exceptions
{
    /// <summary>
    /// Unit tests for transaction exception classes.
    /// </summary>
    public class TransactionExceptionTests
    {
        [Fact]
        public void TransactionException_WithMessage_SetsMessageProperty()
        {
            // Arrange
            var message = "Transaction failed";

            // Act
            var exception = new TransactionException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.ErrorCode);
            Assert.Null(exception.TransactionId);
        }

        [Fact]
        public void TransactionException_WithMessageAndInner_SetsProperties()
        {
            // Arrange
            var message = "Transaction failed";
            var inner = new InvalidOperationException("Inner error");

            // Act
            var exception = new TransactionException(message, inner);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(inner, exception.InnerException);
            Assert.Null(exception.ErrorCode);
            Assert.Null(exception.TransactionId);
        }

        [Fact]
        public void TransactionException_WithCodeAndTxId_SetsAllProperties()
        {
            // Arrange
            var message = "Transaction failed";
            var errorCode = 500;
            var txId = "tx123456";

            // Act
            var exception = new TransactionException(message, errorCode, txId);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(txId, exception.TransactionId);
        }

        [Fact]
        public void TransactionValidationException_WithMessage_SetsMessageProperty()
        {
            // Arrange
            var message = "Validation failed";

            // Act
            var exception = new TransactionValidationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.IsType<TransactionValidationException>(exception);
            Assert.IsAssignableFrom<TransactionException>(exception);
        }

        [Fact]
        public void TransactionValidationException_WithMessageAndInner_SetsProperties()
        {
            // Arrange
            var message = "Validation failed";
            var inner = new ArgumentException("Invalid argument");

            // Act
            var exception = new TransactionValidationException(message, inner);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(inner, exception.InnerException);
        }

        [Fact]
        public void TransactionDeliveryException_WithPendingStatus_SetsProperties()
        {
            // Arrange
            var message = "Delivery failed";
            var isPending = true;
            var txId = "tx789";

            // Act
            var exception = new TransactionDeliveryException(message, isPending, txId);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.True(exception.IsPending);
            Assert.Equal(txId, exception.TransactionId);
        }

        [Fact]
        public void TransactionDeliveryException_DefaultsToNotPending()
        {
            // Arrange
            var message = "Delivery failed";

            // Act
            var exception = new TransactionDeliveryException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.False(exception.IsPending);
            Assert.Null(exception.TransactionId);
        }

        [Fact]
        public void TransactionResponseException_WithCodeAndTxId_SetsAllProperties()
        {
            // Arrange
            var message = "Response error";
            var errorCode = 404;
            var txId = "tx456";

            // Act
            var exception = new TransactionResponseException(message, errorCode, txId);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(txId, exception.TransactionId);
        }

        [Fact]
        public void TransactionResponseException_WithoutTxId_LeavesItNull()
        {
            // Arrange
            var message = "Response error";
            var errorCode = 500;

            // Act
            var exception = new TransactionResponseException(message, errorCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Null(exception.TransactionId);
        }

        [Fact]
        public void TransactionDeserializationException_WithType_SetsTargetType()
        {
            // Arrange
            var message = "Failed to deserialize";
            var targetType = typeof(string);

            // Act
            var exception = new TransactionDeserializationException(message, targetType);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(targetType, exception.TargetType);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void TransactionDeserializationException_WithInnerException_SetsProperties()
        {
            // Arrange
            var message = "Failed to deserialize";
            var targetType = typeof(int);
            var inner = new FormatException("Invalid format");

            // Act
            var exception = new TransactionDeserializationException(message, targetType, inner);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(targetType, exception.TargetType);
            Assert.Equal(inner, exception.InnerException);
        }

        [Fact]
        public void TransactionDeserializationException_WithOnlyMessage_SetsMinimalProperties()
        {
            // Arrange
            var message = "Deserialization error";

            // Act
            var exception = new TransactionDeserializationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.TargetType);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void AllExceptions_InheritFromTransactionException()
        {
            // Arrange & Act
            var validationEx = new TransactionValidationException("test");
            var deliveryEx = new TransactionDeliveryException("test");
            var responseEx = new TransactionResponseException("test", 500);
            var deserializationEx = new TransactionDeserializationException("test");

            // Assert
            Assert.IsAssignableFrom<TransactionException>(validationEx);
            Assert.IsAssignableFrom<TransactionException>(deliveryEx);
            Assert.IsAssignableFrom<TransactionException>(responseEx);
            Assert.IsAssignableFrom<TransactionException>(deserializationEx);
        }
    }
}