using System;
using NSec.Cryptography; // For Key, SignatureAlgorithm
using Acme.Net.Sdk.Protocol.Generated; // For AccountType, SignatureType
using Acme.Net.Sdk.Signing; // For the centralized SignatureKeyPair

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a principal associated with an Accumulate Lite Identity.
    /// A Lite Identity URL is derived directly from the public key.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.LiteIdentityPrincipal.
    /// </summary>
    public class LiteIdentityPrincipal : Principal
    {
        /// <summary>
        /// Gets the Lite Identity Account associated with this principal.
        /// </summary>
        public LiteIdentity LiteIdentity => Account as LiteIdentity ?? 
            throw new InvalidOperationException("Account is not a LiteIdentity");

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteIdentityPrincipal"/> class from an existing key pair.
        /// Computes the Lite Identity URL from the public key.
        /// </summary>
        /// <param name="keyPair">The key pair for this principal.</param>
        /// <exception cref="ArgumentNullException">Thrown if keyPair is null.</exception>
        public LiteIdentityPrincipal(Acme.Net.Sdk.Signing.SignatureKeyPair keyPair) 
            : base(CreateLiteIdentity(keyPair), keyPair)
        {
            // Base constructor handles null check for keyPair via its property access
        }

        // Helper method to create a LiteIdentity with computed URL
        private static LiteIdentity CreateLiteIdentity(Acme.Net.Sdk.Signing.SignatureKeyPair keyPair)
        {
            if (keyPair == null) throw new ArgumentNullException(nameof(keyPair));
            var url = ComputeUrl(keyPair.GetPublicKey());
            return new LiteIdentity(url);
        }

        /// <summary>
        /// Generates a new random Lite Identity Principal with the specified signature type.
        /// </summary>
        /// <param name="signatureType">The type of signature algorithm to use (e.g., ED25519).</param>
        /// <returns>A new <see cref="LiteIdentityPrincipal"/> with a generated key pair.</returns>
        /// <exception cref="NotSupportedException">Thrown if the specified signature type is not supported for key generation.</exception>
        public static LiteIdentityPrincipal Generate(SignatureType signatureType)
        {
            SignatureAlgorithm algorithm;
            if (signatureType == SignatureType.ED25519) {
                algorithm = SignatureAlgorithm.Ed25519;
            }
            // TODO: Add support for other signature types if needed
            else {
                 throw new NotSupportedException($"Generating key pairs for signature type {signatureType} is not currently supported.");
            }

            // Create a new key using NSec
            // Use CreationParameters with ExportPolicy = KeyExportPolicies.AllowPlaintextExport for GetPrivateKey()
            var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
            using var key = Key.Create(algorithm, creationParams);

            // Create our SignatureKeyPair wrapper
            var keyPair = new Acme.Net.Sdk.Signing.SignatureKeyPair(key, signatureType);
            
            return new LiteIdentityPrincipal(keyPair);
        }

        /// <summary>
        /// Exports the key pair associated with this principal to a base64 string, indicating Lite Identity type.
        /// </summary>
        /// <returns>Base64 encoded string of the key pair information.</returns>
        public string ExportToBase64()
        {
            return base.ExportToBase64(AccountType.LITE_IDENTITY);
        }

        /// <summary>
        /// Imports a LiteIdentityPrincipal from a base64 encoded key pair string.
        /// </summary>
        /// <param name="data">The base64 encoded key pair data.</param>
        /// <returns>A new <see cref="LiteIdentityPrincipal"/> instance.</returns>
        public static LiteIdentityPrincipal ImportFromBase64(string data)
        {
            var keyPair = ImportKeyPairFromBase64(data); // Call protected static method from base
            return new LiteIdentityPrincipal(keyPair);
        }
    }
}

