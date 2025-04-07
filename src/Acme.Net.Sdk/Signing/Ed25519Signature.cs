using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;
using NSec.Cryptography;
using System;
using System.IO;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Implementation of Ed25519 signatures for Accumulate.
    /// </summary>
    public class Ed25519Signature : BaseSignature
    {
        /// <summary>
        /// Gets the signature type.
        /// </summary>
        public override SignatureType Type => SignatureType.ED25519;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ed25519Signature"/> class.
        /// </summary>
        public Ed25519Signature()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ed25519Signature"/> class with the specified URL and public key.
        /// </summary>
        /// <param name="url">The signer URL.</param>
        /// <param name="publicKey">The public key bytes.</param>
        public Ed25519Signature(Url url, byte[] publicKey)
        {
            SignerUrl = url ?? throw new ArgumentNullException(nameof(url));
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <summary>
        /// Performs the cryptographic signing operation using Ed25519.
        /// </summary>
        /// <param name="txHash">The hash of the transaction body.</param>
        /// <param name="metadataHash">The hash of the marshalled signature metadata.</param>
        /// <param name="keyPair">The key pair to use for signing.</param>
        /// <exception cref="ArgumentException">Thrown if the key pair type doesn't match the signature type.</exception>
        public override void Sign(byte[] txHash, byte[] metadataHash, SignatureKeyPair keyPair)
        {
            if (keyPair.Type != SignatureType.ED25519)
            {
                throw new ArgumentException($"Expected key pair of type ED25519, got {keyPair.Type}", nameof(keyPair));
            }

            if (txHash == null || txHash.Length == 0)
            {
                throw new ArgumentException("Transaction hash cannot be null or empty", nameof(txHash));
            }

            // Store the transaction hash
            TransactionHash = txHash;

            // Extract the key from the key pair
            Key key = keyPair.GetKey();
            
            // Extract the public key if not already set
            if (PublicKey.Length == 0)
            {
                PublicKey = keyPair.GetPublicKey();
            }

            // Combine the transaction hash and metadata hash for signing
            using (var stream = new MemoryStream())
            {
                stream.Write(txHash, 0, txHash.Length);
                if (metadataHash != null && metadataHash.Length > 0)
                {
                    stream.Write(metadataHash, 0, metadataHash.Length);
                }
                
                byte[] dataToSign = stream.ToArray();
                
                // Use NSec to sign the data
                SignatureAlgorithm algorithm = SignatureAlgorithm.Ed25519;
                SignatureBytes = algorithm.Sign(key, dataToSign);
            }
        }

        /// <summary>
        /// Marshals the signature object into its binary representation.
        /// </summary>
        /// <returns>A byte array containing the marshalled signature.</returns>
        public override byte[] MarshalBinary()
        {
            // Combine metadata and signature bytes
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    // Write version
                    writer.Write(Version);
                    
                    // Write timestamp
                    writer.Write(Timestamp);
                    
                    // Write signer URL
                    byte[] signerBytes = SignerUrlBytes;
                    writer.Write(signerBytes.Length);
                    writer.Write(signerBytes);
                    
                    // Write public key
                    writer.Write(PublicKey.Length);
                    writer.Write(PublicKey);
                    
                    // Write signature bytes
                    writer.Write(SignatureBytes.Length);
                    writer.Write(SignatureBytes);
                }
                
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Gets the underlying generated signature model object.
        /// </summary>
        /// <returns>The generated Signature object.</returns>
        public override Protocol.Generated.Signature GetModel()
        {
            // TODO: Implement when Protocol.Generated.Signature is available
            // For now, return a placeholder that will need to be updated
            return new Protocol.Generated.Signature
            {
                // Fill in required properties
                Type = (int)Type,
                PublicKey = PublicKey,
                SignerUrl = SignerUrl?.String(),
                Signature1 = SignatureBytes,
                TransactionHash = TransactionHash
            };
        }
    }
} 