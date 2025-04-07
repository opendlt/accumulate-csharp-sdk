using System;
using Acme.Net.Sdk.Commons.Codec;
using Xunit;

namespace Acme.Net.Sdk.Tests.Commons.Codec
{
    public class EncoderExceptionTests
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            var ex = new EncoderException();
            Assert.NotNull(ex.Message); // Default message is usually provided by the base Exception class
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void TestMessageConstructor()
        {
            const string message = "Test encoder error";
            var ex = new EncoderException(message);
            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void TestMessageAndInnerExceptionConstructor()
        {
            const string message = "Test encoder error with inner";
            var innerEx = new ArgumentException("Inner problem");
            var ex = new EncoderException(message, innerEx);
            Assert.Equal(message, ex.Message);
            Assert.Same(innerEx, ex.InnerException);
        }

        [Fact]
        public void TestInnerExceptionConstructor()
        {
            var innerEx = new ArgumentException("Inner problem");
            var ex = new EncoderException(innerEx);
            // The C# implementation uses the inner exception's message
            Assert.Equal(innerEx.Message, ex.Message);
            Assert.Same(innerEx, ex.InnerException);
        }

        [Fact]
        public void TestInnerExceptionConstructor_NullInner()
        {
            Exception? innerEx = null;
            var ex = new EncoderException(innerEx!);
            // Base exception message might vary, ensure InnerException is null
            Assert.NotNull(ex.Message);
            Assert.Null(ex.InnerException);
        }
    }
} 