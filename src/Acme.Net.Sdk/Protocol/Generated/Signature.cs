namespace Acme.Net.Sdk.Protocol.Generated
{
    /// <summary>
    /// Placeholder for the generated Signature class.
    /// This will be replaced by the actual generated class when available.
    /// </summary>
    public class Signature
    {
        /// <summary>
        /// Gets or sets the type of the signature.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        public byte[] PublicKey { get; set; } = System.Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the signature bytes.
        /// </summary>
        public byte[] Signature1 { get; set; } = System.Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the signer URL.
        /// </summary>
        public string? SignerUrl { get; set; }

        /// <summary>
        /// Gets or sets the transaction hash.
        /// </summary>
        public byte[] TransactionHash { get; set; } = System.Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public int Version { get; set; } = 1;
    }
} 