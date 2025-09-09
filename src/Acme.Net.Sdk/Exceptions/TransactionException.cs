using System;

namespace Acme.Net.Sdk.Exceptions
{
    /// <summary>
    /// Base exception for all transaction-related errors in the Acme SDK.
    /// </summary>
    public class TransactionException : Exception
    {
        /// <summary>
        /// Gets the error code associated with the transaction failure.
        /// </summary>
        public int? ErrorCode { get; }

        /// <summary>
        /// Gets the transaction ID if available.
        /// </summary>
        public string? TransactionId { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public TransactionException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public TransactionException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class with error code.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="transactionId">The transaction ID.</param>
        public TransactionException(string message, int errorCode, string? transactionId = null) 
            : base(message)
        {
            ErrorCode = errorCode;
            TransactionId = transactionId;
        }
    }

    /// <summary>
    /// Exception thrown when a transaction validation fails.
    /// </summary>
    public class TransactionValidationException : TransactionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionValidationException"/> class.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        public TransactionValidationException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionValidationException"/> class.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public TransactionValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a transaction fails to be delivered.
    /// </summary>
    public class TransactionDeliveryException : TransactionException
    {
        /// <summary>
        /// Gets whether the transaction is still pending.
        /// </summary>
        public bool IsPending { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionDeliveryException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="isPending">Whether the transaction is still pending.</param>
        /// <param name="transactionId">The transaction ID.</param>
        public TransactionDeliveryException(string message, bool isPending = false, string? transactionId = null) 
            : base(message)
        {
            IsPending = isPending;
            TransactionId = transactionId;
        }
    }

    /// <summary>
    /// Exception thrown when a transaction response contains an error.
    /// </summary>
    public class TransactionResponseException : TransactionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionResponseException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code from the response.</param>
        /// <param name="transactionId">The transaction ID.</param>
        public TransactionResponseException(string message, int errorCode, string? transactionId = null) 
            : base(message, errorCode, transactionId)
        {
        }
    }

    /// <summary>
    /// Exception thrown when deserialization of transaction data fails.
    /// </summary>
    public class TransactionDeserializationException : TransactionException
    {
        /// <summary>
        /// Gets the type that failed to deserialize.
        /// </summary>
        public Type? TargetType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionDeserializationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="targetType">The type that failed to deserialize.</param>
        /// <param name="innerException">The inner exception.</param>
        public TransactionDeserializationException(string message, Type? targetType = null, Exception? innerException = null) 
            : base(message, innerException!)
        {
            TargetType = targetType;
        }
    }
}