using Acme.Net.Sdk.Protocol.Generated;
using System;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Factory class for creating signature executors based on signature type.
    /// </summary>
    public static class SignatureExecutorFactory
    {
        private static readonly Dictionary<SignatureType, ISignatureExecutor> _executors = new Dictionary<SignatureType, ISignatureExecutor>();

        /// <summary>
        /// Static constructor to register all the available signature executors.
        /// </summary>
        static SignatureExecutorFactory()
        {
            // Register executors
            RegisterExecutor(new Ed25519SignatureExecutor());
            // Add additional executors as they are implemented
            // RegisterExecutor(new RCD1SignatureExecutor());
        }

        /// <summary>
        /// Registers a signature executor.
        /// </summary>
        /// <param name="executor">The executor to register.</param>
        public static void RegisterExecutor(ISignatureExecutor executor)
        {
            _executors[executor.Type] = executor;
        }

        /// <summary>
        /// Gets a signature executor for the specified signature type.
        /// </summary>
        /// <param name="type">The signature type.</param>
        /// <returns>The signature executor for the specified type.</returns>
        /// <exception cref="NotSupportedException">Thrown if the signature type is not supported.</exception>
        public static ISignatureExecutor GetExecutor(SignatureType type)
        {
            if (_executors.TryGetValue(type, out var executor))
            {
                return executor;
            }

            throw new NotSupportedException($"Signature type {type} is not supported.");
        }

        /// <summary>
        /// Creates a new signature instance for the specified signature type.
        /// </summary>
        /// <param name="type">The signature type.</param>
        /// <returns>A new ISignature instance.</returns>
        public static ISignature CreateSignature(SignatureType type)
        {
            return GetExecutor(type).CreateSignature();
        }
    }
} 