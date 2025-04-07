namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Specifies how the initiator hash in a transaction header should be calculated.
    /// Corresponds to io.accumulatenetwork.sdk.protocol.InitHashMode in Java.
    /// </summary>
    public enum InitHashMode
    {
        /// <summary>
        /// Initialize the transaction initiator field with the Merkle hash of the signature metadata.
        /// </summary>
        INIT_WITH_MERKLE_HASH,

        /// <summary>
        /// Initialize the transaction initiator field with the simple SHA256 hash of the signature metadata.
        /// </summary>
        INIT_WITH_SIMPLE_HASH
    }
}




