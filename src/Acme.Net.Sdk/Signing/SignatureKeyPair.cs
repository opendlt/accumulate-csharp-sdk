using NSec.Cryptography;
using Acme.Net.Sdk.Protocol.Generated; // Reverted to correct namespace for SignatureType
using System;
using System.Diagnostics.CodeAnalysis; // For TryParse

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Represents a key pair used for a specific Accumulate signature type.
    /// Encapsulates the NSec Key object and the associated SignatureType.
    /// </summary>
    public class SignatureKeyPair
    {
        private readonly Key _key;

        /// <summary>
        /// Gets the Accumulate signature type associated with this key pair.
        /// </summary>
        public SignatureType Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignatureKeyPair"/> class.
        /// Internal constructor to force creation via factory method for imported keys.
        /// </summary>
        internal SignatureKeyPair(Key key, SignatureType type)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            Type = type;
        }

        /// <summary>
        /// Imports a key pair from raw secret key bytes and the signature type.
        /// </summary>
        /// <param name="secretKey">The raw secret key bytes.</param>
        /// <param name="type">The signature type.</param>
        /// <param name="keyPair">The resulting key pair if import is successful.</param>
        /// <returns>True if the key was imported successfully, false otherwise.</returns>
        /// <exception cref="NotSupportedException">Thrown if the signature type is not supported for import.</exception>
        public static bool TryImportFromSecretKeyBytes(byte[] secretKey, SignatureType type, [MaybeNullWhen(false)] out SignatureKeyPair keyPair)
        {
            keyPair = null;
            if (secretKey == null) return false;

            SignatureAlgorithm algorithm;
            KeyBlobFormat importFormat = KeyBlobFormat.RawPrivateKey;

            switch (type)
            {
                case SignatureType.ED25519:
                    algorithm = SignatureAlgorithm.Ed25519;
                    // NSec expects 32 bytes for Ed25519 RawPrivateKey
                    if (secretKey.Length != 32) return false; 
                    break;
                // Add RCD1 case if needed (might be same as ED25519?)
                default:
                    throw new NotSupportedException($"Importing key pairs for signature type {type} is not currently supported.");
            }

            var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
            Key? importedKey = null;
            
            try
            {
                importedKey = Key.Import(algorithm, secretKey, importFormat, creationParams);
                keyPair = new SignatureKeyPair(importedKey, type);
                return true;
            }
            catch
            {
                keyPair = null;
                return false;
            }
        }
        
        /// <summary>
        /// Exports the private key bytes if permitted by the key's export policy.
        /// </summary>
        /// <returns>The raw private key bytes.</returns>
        /// <exception cref="NotSupportedException">Thrown if the key does not support export.</exception>
        public byte[] GetPrivateKeyBytes()
        { 
             // Use standard export method, requires KeyExportPolicies.AllowPlaintextExport
            if (!_key.TryExport(KeyBlobFormat.RawPrivateKey, Span<byte>.Empty, out _))
            {
                 throw new NotSupportedException("Private key export is not permitted for this key.");
            }
            return _key.Export(KeyBlobFormat.RawPrivateKey);
        }

        /// <summary>
        /// Gets the public key bytes.
        /// </summary>
        /// <returns>A byte array containing the public key.</returns>
        public byte[] GetPublicKey()
        {
            // Exporting the public key in raw format.
            return _key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        }

        /// <summary>
        /// Gets the underlying NSec Key object.
        /// Use with caution, as it may provide access to private key material depending on how the Key was created.
        /// </summary>
        /// <returns>The NSec Key object.</returns>
        internal Key GetKey()
        {
             return _key;
        }
        
        // Consider adding Sign/Verify methods here later if needed.
    }
}
