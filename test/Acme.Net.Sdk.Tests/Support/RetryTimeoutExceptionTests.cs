using System;
using Acme.Net.Sdk.Support;
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    public class RetryTimeoutExceptionTests
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            var ex = new RetryTimeoutException();
            Assert.NotNull(ex.Message); // Base Exception provides a default message
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void TestMessageConstructor()
        {
            const string message = "Retry operation timed out.";
            var ex = new RetryTimeoutException(message);
            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void TestMessageAndInnerExceptionConstructor()
        {
            const string message = "Timeout during retry.";
            var innerEx = new TimeoutException("Inner timeout");
            var ex = new RetryTimeoutException(message, innerEx);
            Assert.Equal(message, ex.Message);
            Assert.Same(innerEx, ex.InnerException);
        }
    }
} 