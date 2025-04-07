using System;

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Exception thrown when a retry operation exceeds its configured timeout.
    /// </summary>
    [Serializable]
    public class RetryTimeoutException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryTimeoutException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RetryTimeoutException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryTimeoutException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RetryTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryTimeoutException"/> class.
        /// </summary>
        public RetryTimeoutException() : base("Retry operation timed out")
        {
        }
    }
} 