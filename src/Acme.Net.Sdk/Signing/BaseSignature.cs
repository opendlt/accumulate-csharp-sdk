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
        public ulong Timestamp { get; set; }

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

            // Canonical order the node expects:
            // 1) type  2) timestamp  3) signer URL  4) signer version  5) public key
            builder.AddUInt((ulong)Type);
            builder.AddUInt((ulong)Timestamp);

            if (SignerUrl != null)
                builder.AddUrl(SignerUrl);

            builder.AddUInt((ulong)Version);

            if (PublicKey.Length > 0)
                builder.AddBytes(PublicKey);

            return builder;
        }

        /// <summary>
        /// Performs the cryptographic signing operation.
        /// </summary>
        public abstract void Sign(byte[] txHash, byte[] metadataHash, SignatureKeyPair keyPair);

        /// <summary>
        /// Encode only the signature metadata TLV (01,02,04,05,06).
        /// Implemented by concrete signature types.
        /// </summary>
        public abstract byte[] MarshalMetadata();

        /// <summary>
        /// Marshals the full signature (including signature bytes and tx hash).
        /// </summary>
        public abstract byte[] MarshalBinary();

        /// <summary>
        /// Gets the underlying generated signature model object.
        /// </summary>
        public abstract Protocol.Generated.Signature GetModel();
    }
} 