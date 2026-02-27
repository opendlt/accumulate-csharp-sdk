using System.Text.Json;

namespace Acme.Net.Sdk.Exceptions
{
    /// <summary>
    /// Base exception for all Accumulate SDK errors.
    /// Used by the V3 client and unified facade.
    /// </summary>
    public class AccumulateException : Exception
    {
        public AccumulateException(string message)
            : base(message) { }

        public AccumulateException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when the Accumulate API returns a JSON-RPC error response.
    /// Wraps the error code, message, and optional data from the response.
    /// </summary>
    public class AccumulateApiException : AccumulateException
    {
        /// <summary>
        /// The JSON-RPC error code.
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Optional structured error data from the JSON-RPC response.
        /// </summary>
        public new JsonElement? Data { get; }

        public AccumulateApiException(int code, string message, JsonElement? data = null)
            : base(message)
        {
            Code = code;
            Data = data;
        }

        public AccumulateApiException(int code, string message, JsonElement? data, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
            Data = data;
        }

        public override string ToString()
        {
            return $"AccumulateApiException: code={Code}, message={Message}";
        }
    }

    /// <summary>
    /// Thrown when a network connectivity or HTTP-level error occurs.
    /// </summary>
    public class AccumulateNetworkException : AccumulateException
    {
        /// <summary>
        /// The HTTP status code, if available.
        /// </summary>
        public int? StatusCode { get; }

        public AccumulateNetworkException(string message, int? statusCode = null)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public AccumulateNetworkException(string message, Exception innerException, int? statusCode = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Thrown when input validation fails before making an API call.
    /// </summary>
    public class AccumulateValidationException : AccumulateException
    {
        /// <summary>
        /// The name of the parameter that failed validation, if applicable.
        /// </summary>
        public string? ParameterName { get; }

        public AccumulateValidationException(string message, string? parameterName = null)
            : base(message)
        {
            ParameterName = parameterName;
        }

        public AccumulateValidationException(string message, Exception innerException, string? parameterName = null)
            : base(message, innerException)
        {
            ParameterName = parameterName;
        }
    }

    /// <summary>
    /// Thrown when a transaction delivery or polling operation times out.
    /// </summary>
    public class AccumulateTimeoutException : AccumulateException
    {
        /// <summary>
        /// The transaction ID that timed out, if available.
        /// </summary>
        public string? TransactionId { get; }

        public AccumulateTimeoutException(string message, string? transactionId = null)
            : base(message)
        {
            TransactionId = transactionId;
        }

        public AccumulateTimeoutException(string message, Exception innerException, string? transactionId = null)
            : base(message, innerException)
        {
            TransactionId = transactionId;
        }
    }

    /// <summary>
    /// Thrown when marshalling or unmarshalling of protocol data fails.
    /// </summary>
    public class AccumulateEncodingException : AccumulateException
    {
        public AccumulateEncodingException(string message)
            : base(message) { }

        public AccumulateEncodingException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
