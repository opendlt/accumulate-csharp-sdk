using System;

namespace Acme.Net.Sdk.Protocol.Generated
{
    /// <summary>
    /// Represents RPC method types available in the Acme API.
    /// This is a placeholder implementation that will be expanded later.
    /// </summary>
    public class RPCMethod
    {
        /// <summary>
        /// Gets the API method name to be used in RPC calls.
        /// </summary>
        public string ApiMethod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCMethod"/> class.
        /// </summary>
        /// <param name="apiMethod">The API method name.</param>
        private RPCMethod(string apiMethod)
        {
            ApiMethod = apiMethod ?? throw new ArgumentNullException(nameof(apiMethod));
        }

        /// <summary>
        /// ExecuteDirect method for sending transactions.
        /// </summary>
        public static readonly RPCMethod ExecuteDirect = new RPCMethod("execute-direct");

        /// <summary>
        /// Gets the appropriate RPC method for the given transaction body type.
        /// </summary>
        /// <param name="type">The type of transaction body.</param>
        /// <returns>The corresponding RPC method.</returns>
        public static RPCMethod FromClass(Type type)
        {
            // This is a simplified implementation.
            // In a real implementation, it would map transaction types to API methods.
            // For now, we'll just return ExecuteDirect for all types.
            return ExecuteDirect;
        }
    }
} 