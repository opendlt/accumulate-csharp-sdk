using System;
using System.Runtime.Serialization;

namespace Acme.Net.Sdk.Rpc
{
    /// <summary>
    /// Exception thrown when a resource is not found during an RPC request.
    /// Corresponds to io.accumulatenetwork.sdk.rpc.NotFoundException.
    /// </summary>
    [Serializable]
    public class NotFoundException : RPCException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with the specified error code and message.
        /// </summary>
        /// <param name="code">The error code for this exception.</param>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public NotFoundException(int code, string message)
            : base(code, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with an error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public NotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class for serialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        [Obsolete("This API supports obsolete formatter-based serialization.")]
        protected NotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
} 