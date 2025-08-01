using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;
using System;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Base implementation of the ISignature interface.
    /// Provides common functionality for all signature types.
    /// </summary>
    public abstract class BaseSignature : ISignature
    {
        /// <summary>
        /// Gets or sets the public key associated with the signature.
        /// </summary>
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the signature bytes.
        /// </summary>
        public byte[] SignatureBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the signer URL.
        /// </summary>
        public Url? SignerUrl { get; set; }

        /// <summary>
        /// Gets or sets the transaction hash that this signature applies to.
        /// </summary>
        public byte[] TransactionHash { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the timestamp (nonce) for the signature.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the version of the signature.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets the type of this signature.
        /// </summary>
        public abstract SignatureType Type { get; }

        /// <summary>
        /// Gets the raw bytes of the signer URL.
        /// </summary>
        public byte[] SignerUrlBytes => SignerUrl?.GetBytes() ?? Array.Empty<byte>();

        /// <summary>
        /// Gets the core metadata signature, potentially unwrapping delegation layers.
        /// For basic signatures, this returns the signature itself.
        /// </summary>
        /// <returns>The metadata signature.</returns>
        public virtual ISignature GetMetadata()
        {
            return this;
        }

        /// <summary>
        /// Gets a HashBuilder initialized with the necessary data to compute
        /// the transaction initiator hash based on this signature's metadata.
        /// </summary>
        /// <returns>A HashBuilder instance.</returns>
        public virtual HashBuilder GetInitiatorHashBuilder()
        {
            var builder = new HashBuilder();
            
            // Add signature type first
            builder.AddUInt((ulong)Type);
            
            // Then add public key
            if (PublicKey.Length > 0)
            {
                builder.AddBytes(PublicKey);
            }
            
            // Then add signer URL
            if (SignerUrl != null)
            {
                builder.AddUrl(SignerUrl);
            }
            
            // Then add version using AddUInt
            builder.AddUInt(Version);
            
            // Then add timestamp using AddUInt
            builder.AddUInt(Timestamp);
            
            return builder;
        }

        /// <summary>
        /// Performs the cryptographic signing operation.
        /// </summary>
        /// <param name="txHash">The hash of the transaction body.</param>
        /// <param name="metadataHash">The hash of the marshalled signature metadata.</param>
        /// <param name="keyPair">The key pair to use for signing.</param>
        public abstract void Sign(byte[] txHash, byte[] metadataHash, SignatureKeyPair keyPair);

        /// <summary>
        /// Marshals the signature object into its binary representation.
        /// </summary>
        /// <returns>A byte array containing the marshalled signature.</returns>
        public abstract byte[] MarshalBinary();

        /// <summary>
        /// Gets the underlying generated signature model object.
        /// </summary>
        /// <returns>The generated Signature object.</returns>
        public abstract Protocol.Generated.Signature GetModel();
    }
} 