using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Provides a simple mechanism to retry an operation until it succeeds or a timeout is reached.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.support.Retry.
    /// </summary>
    public class Retry
    {
        private TimeSpan? _timeout;
        private TimeSpan? _delay;
        private string? _message;

        /// <summary>
        /// Sets the total timeout duration for the retry operation.
        /// </summary>
        /// <param name="timeout">The maximum time to keep retrying.</param>
        /// <returns>The Retry instance for chaining.</returns>
        public Retry WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the delay duration between retry attempts.
        /// </summary>
        /// <param name="delay">The time to wait between retries.</param>
        /// <returns>The Retry instance for chaining.</returns>
        public Retry WithDelay(TimeSpan delay)
        {
            _delay = delay;
            return this;
        }

        /// <summary>
        /// Sets a custom message format for the <see cref="RetryTimeoutException"/>.
        /// The message can contain a format specifier {0} which will be replaced by the timeout duration.
        /// </summary>
        /// <param name="message">The message format string.</param>
        /// <returns>The Retry instance for chaining.</returns>
        public Retry WithMessage(string message)
        {
            _message = message;
            return this;
        }

        /// <summary>
        /// Executes the callback function repeatedly until it returns false or the timeout is exceeded.
        /// Synchronous version for compatibility with Java code.
        /// </summary>
        /// <param name="callback">A function that performs the operation and returns true if it should be retried, false if successful.</param>
        /// <exception cref="ArgumentNullException">Thrown if callback is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if timeout is not set before execution.</exception>
        /// <exception cref="RetryTimeoutException">Thrown if the operation does not succeed within the specified timeout.</exception>
        public void Execute(Func<bool> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (_timeout == null) throw new InvalidOperationException("Timeout must be configured before executing retry logic.");

            var stopwatch = Stopwatch.StartNew();
            while (callback())
            {
                if (stopwatch.Elapsed >= _timeout.Value)
                {
                    stopwatch.Stop();
                    string timeoutMessage = string.Format(_message ?? "Retry operation timed out after {0}", _timeout.Value.ToString());
                    throw new RetryTimeoutException(timeoutMessage);
                }
                
                if (_delay != null)
                {
                    Thread.Sleep(_delay.Value);
                }
            }
            stopwatch.Stop();
        }

        /// <summary>
        /// Executes the callback function repeatedly until it returns false or the timeout is exceeded.
        /// </summary>
        /// <param name="callback">A function that performs the operation and returns true if it should be retried, false if successful.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if callback is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if timeout is not set before execution.</exception>
        /// <exception cref="RetryTimeoutException">Thrown if the operation does not succeed within the specified timeout.</exception>
        public async Task ExecuteAsync(Func<bool> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (_timeout == null) throw new InvalidOperationException("Timeout must be set before executing retry logic.");

            var stopwatch = Stopwatch.StartNew();
            while (callback())
            {
                if (stopwatch.Elapsed >= _timeout.Value)
                {
                    stopwatch.Stop();
                    string timeoutMessage = string.Format(_message ?? "Retry operation timed out after {0}", _timeout.Value.ToString());
                    throw new RetryTimeoutException(timeoutMessage);
                }
                
                if (_delay != null)
                {
                    await Task.Delay(_delay.Value).ConfigureAwait(false); 
                }
                else
                {
                     // Yield to allow other tasks to run even if no explicit delay is set.
                    await Task.Yield(); 
                }
            }
            stopwatch.Stop();
        }
    }
} 