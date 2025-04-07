using System;
using Acme.Net.Sdk.Protocol.Generated;

namespace Acme.Net.Sdk.Protocol.Signing
{
    /// <summary>
    /// Interface for signing transactions in the Acme protocol
    /// </summary>
    public interface ITransactionSigner
    {
        /// <summary>
        /// Signs a transaction using the specified key data
        /// </summary>
        /// <param name="transaction">The transaction to sign</param>
        /// <param name="privateKeySeed">The ED25519 private key seed as bytes</param>
        /// <param name="timestamp">Optional timestamp to use for the signature (default: current time)</param>
        /// <returns>A TransactionSignature containing the signature data</returns>
        TransactionSignature Sign(Transaction transaction, byte[] privateKeySeed, long? timestamp = null);
        
        /// <summary>
        /// Verifies a transaction signature
        /// </summary>
        /// <param name="transaction">The transaction that was signed</param>
        /// <param name="signature">The signature to verify</param>
        /// <returns>True if the signature is valid, false otherwise</returns>
        bool Verify(Transaction transaction, TransactionSignature signature);
    }
    
    /// <summary>
    /// Represents a signature for a transaction
    /// </summary>
    public class TransactionSignature
    {
        /// <summary>
        /// The signature type (e.g., "ed25519")
        /// </summary>
        public string Type { get; set; } = "ed25519";
        
        /// <summary>
        /// The timestamp when the signature was created
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// The public key bytes in hex format
        /// </summary>
        public string PublicKey { get; set; } = string.Empty;
        
        /// <summary>
        /// The signature bytes in hex format
        /// </summary>
        public string Signature { get; set; } = string.Empty;
        
        /// <summary>
        /// The signer's URL
        /// </summary>
        public string Signer { get; set; } = string.Empty;
        
        /// <summary>
        /// The signer's version
        /// </summary>
        public int SignerVersion { get; set; } = 1;
        
        /// <summary>
        /// The transaction hash in hex format
        /// </summary>
        public string TransactionHash { get; set; } = string.Empty;
    }
} 