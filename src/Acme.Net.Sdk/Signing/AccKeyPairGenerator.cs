using NSec.Cryptography;
using Acme.Net.Sdk.Protocol.Generated; // Reverted to correct namespace for SignatureType
using System;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Utility class for generating Accumulate compatible key pairs.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.signing.AccKeyPairGenerator.
    /// </summary>
    public static class AccKeyPairGenerator
    {
        /// <summary>
        /// Generates a new NSec Key object for the specified Accumulate signature type.
        /// </summary>
        /// <param name="signatureType">The desired signature type (e.g., ED25519, RCD1).</param>
        /// <returns>A new NSec Key object.</returns>
        /// <exception cref="ArgumentException">Thrown if the signature type is not supported.</exception>
        public static Key GenerateKey(SignatureType signatureType)
        {
            SignatureAlgorithm algorithm;
            switch (signatureType)
            {
                case SignatureType.ED25519:
                case SignatureType.RCD1: // Treat RCD1 as ED25519 for key generation
                    algorithm = SignatureAlgorithm.Ed25519;
                    break;
                // Add cases for other supported types here if needed
                default:
                     throw new ArgumentException($"Signature type {signatureType} is not supported for key generation.", nameof(signatureType));
            }

            // Use CreationParameters allowing plaintext export, consistent with Principal import/export
            var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
            return Key.Create(algorithm, creationParams);
        }

        /// <summary>
        /// Generates a new SignatureKeyPair (containing an NSec Key and the SignatureType)
        /// for the specified Accumulate signature type.
        /// </summary>
        /// <param name="signatureType">The desired signature type.</param>
        /// <returns>A new SignatureKeyPair.</returns>
        /// <exception cref="ArgumentException">Thrown if the signature type is not supported.</exception>
        public static SignatureKeyPair GenerateSignatureKeyPair(SignatureType signatureType)
        {
            Key key = GenerateKey(signatureType);

            // Export the 32-byte seed and re-import through our canonical class
            var seed32 = key.Export(KeyBlobFormat.RawPrivateKey);

            if (!SignatureKeyPair.TryImportFromSecretKeyBytes(seed32, signatureType, out var keyPair))
                throw new InvalidOperationException("Failed to import generated key into SignatureKeyPair.");

            return keyPair;
        }
    }
} 