using System;
using Acme.Net.Sdk.Commons.Codec;
using Xunit;

namespace Acme.Net.Sdk.Tests.Commons.Codec
{
    public class DecoderExceptionTests
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            var ex = new DecoderException();
            Assert.NotNull(ex.Message); // Default message is usually provided by the base Exception class
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void TestMessageConstructor()
        {
            const string message = "Test decoder error";
            var ex = new DecoderException(message);
            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void TestMessageAndInnerExceptionConstructor()
        {
            const string message = "Test decoder error with inner";
            var innerEx = new InvalidOperationException("Inner problem");
            var ex = new DecoderException(message, innerEx);
            Assert.Equal(message, ex.Message);
            Assert.Same(innerEx, ex.InnerException);
        }

        [Fact]
        public void TestInnerExceptionConstructor()
        {
            var innerEx = new InvalidOperationException("Inner problem");
            var ex = new DecoderException(innerEx);
            // The C# implementation uses the inner exception's message
            Assert.Equal(innerEx.Message, ex.Message);
            Assert.Same(innerEx, ex.InnerException);
        }

        [Fact]
        public void TestInnerExceptionConstructor_NullInner()
        {
            Exception? innerEx = null;
            var ex = new DecoderException(innerEx!);
            // Base exception message might vary, ensure InnerException is null
            Assert.NotNull(ex.Message);
            Assert.Null(ex.InnerException);
        }
    }
} 