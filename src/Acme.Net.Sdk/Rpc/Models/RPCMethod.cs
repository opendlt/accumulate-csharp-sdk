using System;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Rpc.Models
{
    /// <summary>
    /// Represents an RPC method with its corresponding API method name.
    /// </summary>
    public class RPCMethod
    {
        private static readonly Dictionary<Type, RPCMethod> _typeMap = new Dictionary<Type, RPCMethod>();
        
        /// <summary>
        /// Gets the static instance for the execute-direct method.
        /// </summary>
        public static readonly RPCMethod ExecuteDirect = new RPCMethod("execute-direct");

        /// <summary>
        /// Gets the static instance for the query method.
        /// </summary>
        public static readonly RPCMethod Query = new RPCMethod("query");

        /// <summary>
        /// Gets the static instance for the query-chain method.
        /// </summary>
        public static readonly RPCMethod QueryChain = new RPCMethod("query-chain");

        /// <summary>
        /// Gets the API method name.
        /// </summary>
        public string ApiMethod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCMethod"/> class.
        /// </summary>
        /// <param name="apiMethod">The API method name.</param>
        public RPCMethod(string apiMethod)
        {
            ApiMethod = apiMethod ?? throw new ArgumentNullException(nameof(apiMethod));
        }

        /// <summary>
        /// Registers a type with an RPC method.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="method">The RPC method.</param>
        public static void RegisterType(Type type, RPCMethod method)
        {
            _typeMap[type] = method;
        }

        /// <summary>
        /// Gets the RPC method for a type.
        /// </summary>
        /// <param name="type">The type to look up.</param>
        /// <returns>The RPC method.</returns>
        /// <exception cref="ArgumentException">Thrown if the type is not registered.</exception>
        public static RPCMethod FromClass(Type type)
        {
            if (_typeMap.TryGetValue(type, out var method))
            {
                return method;
            }

            throw new ArgumentException($"Unknown RPC method for type {type.Name}", nameof(type));
        }

        /// <summary>
        /// Determines whether two RPCMethod objects are equal.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is RPCMethod other)
            {
                return ApiMethod == other.ApiMethod;
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code for this RPCMethod.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return ApiMethod.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the RPCMethod.
        /// </summary>
        /// <returns>The API method name.</returns>
        public override string ToString()
        {
            return ApiMethod;
        }
    }
} 