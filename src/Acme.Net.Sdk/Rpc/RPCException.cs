using System;
using System.Runtime.Serialization;
using Acme.Net.Sdk.Rpc.Models;

namespace Acme.Net.Sdk.Rpc
{
    /// <summary>
    /// Represents an exception that is thrown when an RPC call fails.
    /// </summary>
    [Serializable]
    public class RPCException : Exception
    {
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Gets the error data.
        /// </summary>
        public object? ErrorData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public RPCException(string message) : base(message)
        {
            Code = -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        public RPCException(int code, string message) : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        /// <param name="error">The RPC error.</param>
        public RPCException(RPCError error) : base(error.ToString())
        {
            Code = error.Code;
            ErrorData = error.Data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        [Obsolete("This API supports obsolete formatter-based serialization.")]
        protected RPCException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Code = info.GetInt32(nameof(Code));
            ErrorData = info.GetValue(nameof(ErrorData), typeof(object));
        }

        /// <summary>
        /// Sets the object data for serialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        [Obsolete("This API supports obsolete formatter-based serialization.")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Code), Code);
            info.AddValue(nameof(ErrorData), ErrorData);
        }

        /// <summary>
        /// Returns a string representation of the exception.
        /// </summary>
        /// <returns>A string describing the exception.</returns>
        public override string ToString()
        {
            return $"RPC Error {Code}: {Message}";
        }
    }
} 