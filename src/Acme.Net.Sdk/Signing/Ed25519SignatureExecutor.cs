using Acme.Net.Sdk.Protocol.Generated;
using NSec.Cryptography;
using System;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Implementation of the signature executor for Ed25519 signatures.
    /// </summary>
    public class Ed25519SignatureExecutor : ISignatureExecutor
    {
        private static readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;

        /// <summary>
        /// Gets the signature type this executor handles.
        /// </summary>
        public SignatureType Type => SignatureType.ED25519;

        /// <summary>
        /// Creates a new signature instance.
        /// </summary>
        /// <returns>A new Ed25519Signature instance.</returns>
        public ISignature CreateSignature()
        {
            return new Ed25519Signature();
        }

        /// <summary>
        /// Signs data using the specified key pair.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="keyPair">The key pair to use for signing.</param>
        /// <returns>The signature bytes.</returns>
        /// <exception cref="ArgumentException">Thrown if the key pair type doesn't match the signature type.</exception>
        public byte[] Sign(byte[] data, SignatureKeyPair keyPair)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data to sign cannot be null or empty", nameof(data));
            }

            if (keyPair.Type != SignatureType.ED25519)
            {
                throw new ArgumentException($"Expected key pair of type ED25519, got {keyPair.Type}", nameof(keyPair));
            }

            Key key = keyPair.GetKey();
            return _algorithm.Sign(key, data);
        }

        /// <summary>
        /// Verifies a signature against the data and public key.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="publicKey">The public key to use for verification.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        public bool Verify(byte[] data, byte[] signature, byte[] publicKey)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            }

            if (signature == null || signature.Length == 0)
            {
                throw new ArgumentException("Signature cannot be null or empty", nameof(signature));
            }

            if (publicKey == null || publicKey.Length == 0)
            {
                throw new ArgumentException("Public key cannot be null or empty", nameof(publicKey));
            }

            try
            {
                // Use NSec's Verify method which returns bool
                if (!PublicKey.TryImport(_algorithm, publicKey, KeyBlobFormat.RawPublicKey, out var nsecPublicKey))
                {
                    return false;
                }

                return nsecPublicKey != null && _algorithm.Verify(nsecPublicKey, data, signature);
            }
            catch
            {
                // Any exception during verification should be treated as failure
                return false;
            }
        }
    }
} 