using System;
using System.Diagnostics;
using System.Threading;
using Acme.Net.Sdk.Support;
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    public class RetryTests
    {
        [Fact]
        public void TestExecute_SuccessFirstTry()
        {
            bool callbackCalled = false;
            Func<bool> successCallback = () => 
            {
                callbackCalled = true;
                return false; // Success
            };

            new Retry()
                .WithTimeout(TimeSpan.FromSeconds(1))
                .Execute(successCallback);

            Assert.True(callbackCalled);
        }

        [Fact]
        public void TestExecute_SuccessAfterRetries()
        {
            int callCount = 0;
            Func<bool> retryCallback = () => 
            {
                callCount++;
                return callCount < 3; // Succeed on the 3rd call (returns false)
            };

            var stopwatch = Stopwatch.StartNew();
            new Retry()
                .WithTimeout(TimeSpan.FromSeconds(5))
                .WithDelay(TimeSpan.FromMilliseconds(50)) // Add a small delay
                .Execute(retryCallback);
            stopwatch.Stop();

            Assert.Equal(3, callCount);
            Assert.True(stopwatch.ElapsedMilliseconds >= 100); // Should have delayed twice (2 * 50ms)
        }

        [Fact]
        public void TestExecute_TimeoutOccurs()
        {
            int callCount = 0;
            Func<bool> alwaysRetryCallback = () => 
            {
                callCount++;
                // Simulate work taking some time to ensure timeout happens
                Thread.Sleep(20);
                return true; // Always retry
            };

            var ex = Assert.Throws<RetryTimeoutException>(() => 
                new Retry()
                    .WithTimeout(TimeSpan.FromMilliseconds(50)) // Short timeout
                    .WithDelay(TimeSpan.FromMilliseconds(10))
                    .Execute(alwaysRetryCallback)
            );
            
            Assert.Contains("00:00:00.0500000", ex.Message); // Default message format includes timeout
            Assert.True(callCount > 0); // Callback should have been called at least once
        }

        [Fact]
        public void TestExecute_TimeoutOccursWithCustomMessage()
        {
            Func<bool> alwaysRetryCallback = () => true; // Always retry
            string customMsgFormat = "Custom timeout after {0}!";
            string expectedMsg = "Custom timeout after 00:00:00.0100000!"; // Timeout is 10ms

            var ex = Assert.Throws<RetryTimeoutException>(() => 
                new Retry()
                    .WithTimeout(TimeSpan.FromMilliseconds(10))
                    .WithMessage(customMsgFormat)
                    .Execute(alwaysRetryCallback)
            );
            
            Assert.Equal(expectedMsg, ex.Message);
        }

        [Fact]
        public void TestExecute_NoTimeoutConfiguredThrows()
        {
             Func<bool> dummyCallback = () => false;

            var ex = Assert.Throws<InvalidOperationException>(() => 
                new Retry() // No WithTimeout call
                    .Execute(dummyCallback)
            );

            Assert.Contains("Timeout must be configured", ex.Message);
        }

        [Fact]
        public void TestExecute_NullCallbackThrows()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => 
                new Retry()
                    .WithTimeout(TimeSpan.FromSeconds(1))
                    .Execute(null!) // Pass null callback
            );

            Assert.Equal("callback", ex.ParamName);
        }

        [Fact]
        public void TestExecute_NoDelayConfigured()
        {
            int callCount = 0;
            Func<bool> retryCallback = () => 
            {
                callCount++;
                return callCount < 2; // Succeed on 2nd call
            };

            // Should execute quickly without significant delay
            var stopwatch = Stopwatch.StartNew();
            new Retry()
                .WithTimeout(TimeSpan.FromSeconds(1))
                // No WithDelay call
                .Execute(retryCallback);
            stopwatch.Stop();

            Assert.Equal(2, callCount);
            Assert.True(stopwatch.ElapsedMilliseconds < 50); // Should be very fast without delay
        }
    }
} 