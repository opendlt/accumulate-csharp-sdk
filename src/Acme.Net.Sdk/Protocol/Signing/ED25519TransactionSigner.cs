using System;
using System.Linq;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol.Generated;
using NSec.Cryptography;

namespace Acme.Net.Sdk.Protocol.Signing
{
    /// <summary>
    /// Implementation of ITransactionSigner that uses ED25519 signatures
    /// </summary>
    public class ED25519TransactionSigner : ITransactionSigner
    {
        private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;
        
        /// <summary>
        /// Signs a transaction using the specified ED25519 private key seed
        /// </summary>
        /// <param name="transaction">The transaction to sign</param>
        /// <param name="privateKeySeed">The ED25519 private key seed as bytes</param>
        /// <param name="timestamp">Optional timestamp to use for the signature (default: current time)</param>
        /// <returns>A TransactionSignature containing the signature data</returns>
        public TransactionSignature Sign(Transaction transaction, byte[] privateKeySeed, long? timestamp = null)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            if (privateKeySeed == null || privateKeySeed.Length == 0)
                throw new ArgumentNullException(nameof(privateKeySeed));
            
            // Import the key from the provided seed
            var key = Key.Import(_algorithm, privateKeySeed, KeyBlobFormat.RawPrivateKey);
            
            // Get the public key
            var publicKey = key.Export(KeyBlobFormat.RawPublicKey);
            string publicKeyHex = new string(Hex.EncodeHex(publicKey)).ToLowerInvariant();
            
            // Compute the transaction hash if not already set
            if (transaction.Hash == null || transaction.Hash.Length == 0)
            {
                transaction.Hash = TransactionHasher.ComputeTransactionHash(transaction);
            }
            
            string txHashHex = new string(Hex.EncodeHex(transaction.Hash)).ToLowerInvariant();
            
            // Use the provided timestamp or current time
            long signatureTimestamp = timestamp ?? (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L);
            
            // Sign the transaction hash
            byte[] signatureBytes = _algorithm.Sign(key, transaction.Hash);
            string signatureHex = new string(Hex.EncodeHex(signatureBytes)).ToLowerInvariant();
            
            // Create the signature object
            return new TransactionSignature
            {
                Type = "ed25519",
                Timestamp = signatureTimestamp,
                PublicKey = publicKeyHex,
                Signature = signatureHex,
                TransactionHash = txHashHex,
                // Note: Signer and SignerVersion should be set by the caller if needed
            };
        }
        
        /// <summary>
        /// Verifies a transaction signature
        /// </summary>
        /// <param name="transaction">The transaction that was signed</param>
        /// <param name="signature">The signature to verify</param>
        /// <returns>True if the signature is valid, false otherwise</returns>
        public bool Verify(Transaction transaction, TransactionSignature signature)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            
            // Check if the signature type is supported
            if (!string.Equals(signature.Type, "ed25519", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Unsupported signature type: {signature.Type}");
            
            // Get the public key
            byte[] publicKeyBytes = Hex.DecodeHex(signature.PublicKey);
            var publicKeyObj = PublicKey.Import(_algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey);
            
            // Get the signature
            byte[] signatureBytes = Hex.DecodeHex(signature.Signature);
            
            // Compute the transaction hash if not already set
            if (transaction.Hash == null || transaction.Hash.Length == 0)
            {
                transaction.Hash = TransactionHasher.ComputeTransactionHash(transaction);
            }
            
            // If the hashes don't match, verification will fail
            byte[] expectedTxHash = Hex.DecodeHex(signature.TransactionHash);
            if (!transaction.Hash.SequenceEqual(expectedTxHash))
            {
                return false;
            }
            
            // Verify the signature
            return _algorithm.Verify(publicKeyObj, transaction.Hash, signatureBytes);
        }
    }
} 