using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Net.Sdk.Protocol.Generated;

namespace Acme.Net.Sdk.Protocol.Signing
{
    /// <summary>
    /// Service responsible for managing transaction signers and providing transaction signing capabilities.
    /// </summary>
    public class TransactionSignerService : IDisposable
    {
        private readonly Dictionary<string, ITransactionSigner> _signers = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionSignerService"/> class.
        /// </summary>
        public TransactionSignerService()
        {
            // Register the ED25519 signer by default
            RegisterSigner("ed25519", new ED25519TransactionSigner());
        }

        /// <summary>
        /// Registers a transaction signer for a specific signature type.
        /// </summary>
        /// <param name="signatureType">The signature type identifier.</param>
        /// <param name="signer">The transaction signer implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when signatureType or signer is null.</exception>
        /// <exception cref="ArgumentException">Thrown when signatureType is empty or whitespace.</exception>
        public void RegisterSigner(string signatureType, ITransactionSigner signer)
        {
            if (string.IsNullOrWhiteSpace(signatureType))
                throw new ArgumentException("Signature type cannot be empty or whitespace.", nameof(signatureType));
            
            if (signer == null)
                throw new ArgumentNullException(nameof(signer));

            _signers[signatureType.ToLowerInvariant()] = signer;
        }

        /// <summary>
        /// Signs a transaction using the specified signature type and private key.
        /// </summary>
        /// <param name="transaction">The transaction to sign.</param>
        /// <param name="signatureType">The signature type to use (e.g., "ed25519").</param>
        /// <param name="privateKeySeed">The private key seed bytes.</param>
        /// <param name="timestamp">Optional timestamp for the signature.</param>
        /// <returns>A TransactionSignature containing the signature data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when signatureType is empty or not registered.</exception>
        public TransactionSignature SignTransaction(Transaction transaction, string signatureType, byte[] privateKeySeed, long? timestamp = null)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            if (string.IsNullOrWhiteSpace(signatureType))
                throw new ArgumentException("Signature type cannot be empty or whitespace.", nameof(signatureType));
            
            if (privateKeySeed == null)
                throw new ArgumentNullException(nameof(privateKeySeed));

            var normalizedType = signatureType.ToLowerInvariant();
            if (!_signers.TryGetValue(normalizedType, out var signer))
                throw new ArgumentException($"No signer registered for signature type: {signatureType}", nameof(signatureType));

            return signer.Sign(transaction, privateKeySeed, timestamp);
        }

        /// <summary>
        /// Verifies a transaction signature.
        /// </summary>
        /// <param name="transaction">The transaction to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when transaction or signature is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the signature type is not registered.</exception>
        public bool VerifySignature(Transaction transaction, TransactionSignature signature)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            var normalizedType = signature.Type.ToLowerInvariant();
            if (!_signers.TryGetValue(normalizedType, out var signer))
                throw new ArgumentException($"No signer registered for signature type: {signature.Type}", nameof(signature));

            return signer.Verify(transaction, signature);
        }

        /// <summary>
        /// Disposes the TransactionSignerService and all registered signers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the TransactionSignerService.
        /// </summary>
        /// <param name="disposing">Whether the method is called from Dispose() or the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var signer in _signers.Values)
                    {
                        if (signer is IDisposable disposableSigner)
                        {
                            disposableSigner.Dispose();
                        }
                    }
                    _signers.Clear();
                }

                _disposed = true;
            }
        }
    }
} 