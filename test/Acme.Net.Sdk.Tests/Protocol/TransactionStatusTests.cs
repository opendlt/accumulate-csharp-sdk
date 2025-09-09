using System;
using Xunit;
using Acme.Net.Sdk.Protocol;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Unit tests for the TransactionStatus class.
    /// </summary>
    public class TransactionStatusTests
    {
        [Fact]
        public void IsSuccess_WithCode0AndNoError_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 0,
                Error = null
            };

            // Act
            var result = status.IsSuccess();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSuccess_WithCode200AndNoError_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 200,
                Error = null
            };

            // Act
            var result = status.IsSuccess();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSuccess_WithCode0AndEmptyError_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 0,
                Error = ""
            };

            // Act
            var result = status.IsSuccess();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSuccess_WithErrorCode_ReturnsFalse()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 500,
                Error = null
            };

            // Act
            var result = status.IsSuccess();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSuccess_WithCode0AndError_ReturnsFalse()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 0,
                Error = "Some error occurred"
            };

            // Act
            var result = status.IsSuccess();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasError_WithErrorMessage_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 0,
                Error = "Error message"
            };

            // Act
            var result = status.HasError();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasError_WithErrorCode_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 404,
                Error = null
            };

            // Act
            var result = status.HasError();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasError_WithNoErrorAndSuccessCode_ReturnsFalse()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 200,
                Error = null
            };

            // Act
            var result = status.HasError();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsComplete_WhenDelivered_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Delivered = true,
                Pending = false
            };

            // Act
            var result = status.IsComplete();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsComplete_WhenDeliveredAndPending_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Delivered = true,
                Pending = true // Delivered takes precedence
            };

            // Act
            var result = status.IsComplete();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsComplete_WhenNotPendingAndHasError_ReturnsTrue()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Delivered = false,
                Pending = false,
                Code = 500,
                Error = "Failed"
            };

            // Act
            var result = status.IsComplete();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsComplete_WhenPending_ReturnsFalse()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Delivered = false,
                Pending = true
            };

            // Act
            var result = status.IsComplete();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetStatusMessage_WithError_ReturnsErrorMessage()
        {
            // Arrange
            var errorMsg = "Transaction failed due to insufficient funds";
            var status = new TransactionStatus
            {
                Error = errorMsg
            };

            // Act
            var result = status.GetStatusMessage();

            // Assert
            Assert.Equal($"Error: {errorMsg}", result);
        }

        [Fact]
        public void GetStatusMessage_WhenDelivered_ReturnsSuccessMessage()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Delivered = true,
                Error = null
            };

            // Act
            var result = status.GetStatusMessage();

            // Assert
            Assert.Equal("Transaction delivered successfully", result);
        }

        [Fact]
        public void GetStatusMessage_WhenPending_ReturnsPendingMessage()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Delivered = false,
                Pending = true,
                Error = null
            };

            // Act
            var result = status.GetStatusMessage();

            // Assert
            Assert.Equal("Transaction is pending", result);
        }

        [Fact]
        public void GetStatusMessage_WithCodeOnly_ReturnsCodeMessage()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Code = 404,
                Delivered = false,
                Pending = false,
                Error = null
            };

            // Act
            var result = status.GetStatusMessage();

            // Assert
            Assert.Equal("Transaction status: Code 404", result);
        }

        [Fact]
        public void GetStatusMessage_ErrorTakesPrecedence()
        {
            // Arrange
            var status = new TransactionStatus
            {
                Error = "Critical error",
                Delivered = true, // Should be ignored when error is present
                Pending = true
            };

            // Act
            var result = status.GetStatusMessage();

            // Assert
            Assert.Equal("Error: Critical error", result);
        }
    }
}