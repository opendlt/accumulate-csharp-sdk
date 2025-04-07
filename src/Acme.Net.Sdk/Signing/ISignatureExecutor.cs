using Acme.Net.Sdk.Protocol.Generated;
using System;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Interface for signature algorithm executors.
    /// Each signature type (ED25519, RCD1, etc.) will have its own implementation.
    /// </summary>
    public interface ISignatureExecutor
    {
        /// <summary>
        /// Gets the signature type this executor handles.
        /// </summary>
        SignatureType Type { get; }

        /// <summary>
        /// Creates a new signature instance.
        /// </summary>
        /// <returns>A new ISignature instance for this signature type.</returns>
        ISignature CreateSignature();

        /// <summary>
        /// Signs data using the specified key pair.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="keyPair">The key pair to use for signing.</param>
        /// <returns>The signature bytes.</returns>
        byte[] Sign(byte[] data, SignatureKeyPair keyPair);

        /// <summary>
        /// Verifies a signature against the data and public key.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="publicKey">The public key to use for verification.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        bool Verify(byte[] data, byte[] signature, byte[] publicKey);
    }
} 