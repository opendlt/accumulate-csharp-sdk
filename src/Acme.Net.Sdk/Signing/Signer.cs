using System;
using System.Collections.Generic;
using Acme.Net.Sdk.Protocol; // For Url
using Acme.Net.Sdk.Protocol.Generated; // Reverted to correct namespace for SignatureType, Transaction
using Acme.Net.Sdk.Support; // For HashBuilder

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Builds and orchestrates the signing process for transactions.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.signing.Signer.
    /// </summary>
    public class Signer
    {
        private InitHashMode _initMode = InitHashMode.INIT_WITH_MERKLE_HASH;
        private SignatureType? _signatureType;
        private Url? _url;
        private int? _version;
        private readonly List<Url> _delegators = new List<Url>();
        private long? _timestamp; // Use long? to check if set
        private SignatureKeyPair? _keyPair; // Use SignatureKeyPair instead of raw private key bytes

        /// <summary>
        /// Sets the signature type for the signer.
        /// </summary>
        public Signer WithType(SignatureType signatureType)
        {
            _signatureType = signatureType;
            return this;
        }

        /// <summary>
        /// Sets the timestamp (nonce) for the signature.
        /// </summary>
        public Signer WithTimeStamp(long timestamp)
        {
            _timestamp = timestamp;
            return this;
        }

        /// <summary>
        /// Sets the timestamp (nonce) based on the current system time (microseconds precision).
        /// </summary>
        public Signer WithNonceFromTimeNow()
        {
            // .NET DateTime ticks are 100-nanosecond intervals. Convert to microseconds.
            long microseconds = DateTime.UtcNow.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
            _timestamp = microseconds;
            return this;
        }

        /// <summary>
        /// Configures the signer to use the simple hash of the metadata as the transaction initiator.
        /// </summary>
        public Signer WithUseSimpleHash()
        {
            _initMode = InitHashMode.INIT_WITH_SIMPLE_HASH;
            return this;
        }

        /// <summary>
        /// Configures the signer to use the Merkle hash of the metadata as the transaction initiator (default).
        /// </summary>
        public Signer WithUseMerkleHash()
        {
            _initMode = InitHashMode.INIT_WITH_MERKLE_HASH;
            return this;
        }

        /// <summary>
        /// Sets the version of the signer.
        /// </summary>
        public Signer WithVersion(int version)
        {
            _version = version;
            return this;
        }

        /// <summary>
        /// Sets the key pair to use for signing.
        /// </summary>
        public Signer WithKeyPair(SignatureKeyPair keyPair)
        {
            _keyPair = keyPair;
            return this;
        }

        /// <summary>
        /// Adds delegator URLs to the signature.
        /// </summary>
        public Signer WithDelegators(IEnumerable<Url> delegators)
        {
            if (delegators != null)
            {
                 _delegators.AddRange(delegators);
            }
            return this;
        }

        /// <summary>
        /// Sets the URL of the signer.
        /// </summary>
        public Signer WithUrl(Url url)
        {
            _url = url;
            return this;
        }

        /// <summary>
        /// Prepares the signature object based on the configured parameters.
        /// </summary>
        /// <param name="init">Indicates if this is for transaction initiation (requires version/timestamp).</param>
        /// <returns>An ISignature object representing the prepared signature.</returns>
        /// <exception cref="InvalidOperationException">Thrown if required parameters are missing.</exception>
        public ISignature Prepare(bool init)
        {
            Validate(init); // Perform validation first

            // Create the signature through the factory
            var signature = SignatureExecutorFactory.CreateSignature(_signatureType.Value);

            // Set up common properties
            if (signature is BaseSignature baseSignature)
            {
                baseSignature.SignerUrl = _url;
                
                if (init && _version.HasValue)
                {
                    baseSignature.Version = _version.Value;
                }
                
                if (init && _timestamp.HasValue)
                {
                    baseSignature.Timestamp = _timestamp.Value;
                }
                
                // Extract the public key if possible
                if (_keyPair != null)
                {
                    baseSignature.PublicKey = _keyPair.GetPublicKey();
                }
            }
            
            return signature;
        }

        /// <summary>
        /// Initiates a transaction by hashing it, setting the initiator hash, and signing the result.
        /// </summary>
        /// <param name="transaction">The transaction to initiate and sign.</param>
        /// <returns>An ISignature object containing the generated signature.</returns>
        /// <exception cref="InvalidOperationException">Thrown if transaction header is null or required parameters are missing.</exception>
        public ISignature Initiate(Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }
            
            if (transaction.Header == null)
            {
                throw new InvalidOperationException("Transaction header cannot be null");
            }
            
            if (transaction.Body == null)
            {
                throw new InvalidOperationException("Transaction body cannot be null");
            }
            
            // Prepare the signature (for initiation, so pass true)
            var signature = Prepare(true);
            
            // Compute transaction hash
            byte[] txHash = TransactionHasher.ComputeTransactionHash(transaction);
            
            // Get metadata hash based on init mode
            byte[] metadataHash = GetMetadataHash(signature);
            
            // Set the initiator hash on the transaction header
            transaction.Header.Initiator = metadataHash;
            
            // Sign the transaction
            signature.Sign(txHash, metadataHash, _keyPair!);
            
            return signature;
        }

        /// <summary>
        /// Signs an existing transaction hash.
        /// </summary>
        /// <param name="hash">The hash to sign.</param>
        /// <returns>An ISignature object containing the generated signature.</returns>
        /// <exception cref="InvalidOperationException">Thrown if required parameters are missing.</exception>
        public ISignature SignAdditional(byte[] hash)
        {
            if (hash == null || hash.Length == 0)
            {
                throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
            }
            
            // Prepare the signature (not for initiation, so pass false)
            var signature = Prepare(false);
            
            // Sign the hash (with empty metadata array since this isn't initiation)
            signature.Sign(hash, new byte[0], _keyPair!);
            
            return signature;
        }

        /// <summary>
        /// Gets the metadata hash based on the initialization mode.
        /// </summary>
        /// <param name="signature">The signature to get the metadata hash for.</param>
        /// <returns>The metadata hash.</returns>
        private byte[] GetMetadataHash(ISignature signature)
        {
            var hashBuilder = signature.GetInitiatorHashBuilder();
            
            switch (_initMode)
            {
                case InitHashMode.INIT_WITH_SIMPLE_HASH:
                    return hashBuilder.GetCheckSum();
                case InitHashMode.INIT_WITH_MERKLE_HASH:
                default:
                    return hashBuilder.MerkleHash();
            }
        }

        private void Validate(bool init)
        {
            if (_url == null) throw new InvalidOperationException("Missing signer URL.");
            if (_keyPair == null) throw new InvalidOperationException("Missing key pair.");
            if (!_signatureType.HasValue) throw new InvalidOperationException("Missing signature type.");
            
            // Ensure key pair type matches specified signature type
            if (_keyPair.Type != _signatureType.Value)
            {
                throw new InvalidOperationException($"Provided key pair type ({_keyPair.Type}) does not match specified signature type ({_signatureType.Value}).");
            }

            if (init)
            {
                if (!_version.HasValue) throw new InvalidOperationException("Missing signer version for initiation.");
                if (!_timestamp.HasValue) throw new InvalidOperationException("Missing timestamp (nonce) for initiation.");
            }
        }
    }

    // --- Placeholder Interfaces/Classes (to be defined properly later) ---
}
