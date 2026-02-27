using Acme.Net.Sdk.Protocol.Generated; // Reverted to correct namespace for Signature, SignatureType
using Acme.Net.Sdk.Protocol; // Keep for Url etc. (if needed)
using Acme.Net.Sdk.Support; // For HashBuilder

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Interface representing an Accumulate signature object.
    /// Defines the contract for different signature types and structures.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.signing.executors.SignExecutor.
    /// </summary>
    public interface ISignature
    {
        /// <summary>
        /// Gets the public key associated with the signature.
        /// </summary>
        byte[] PublicKey { get; }

        /// <summary>
        /// Gets the raw bytes of the signer URL.
        /// Note: Interpretation of getSigner() from Java; might need adjustment based on implementations.
        /// </summary>
        byte[] SignerUrlBytes { get; }

        /// <summary>
        /// Gets the transaction hash that this signature applies to.
        /// </summary>
        byte[] TransactionHash { get; }

        /// <summary>
        /// Gets the type of this signature.
        /// </summary>
        SignatureType Type { get; }

        /// <summary>
        /// Gets the core metadata signature, potentially unwrapping delegation layers.
        /// </summary>
        /// <returns>The metadata <see cref="ISignature"/>.</returns>
        ISignature GetMetadata();

        /// <summary>
        /// Gets a <see cref="HashBuilder"/> initialized with the necessary data 
        /// to compute the transaction initiator hash based on this signature's metadata.
        /// </summary>
        /// <returns>A HashBuilder instance.</returns>
        HashBuilder GetInitiatorHashBuilder();

        /// <summary>
        /// Performs the cryptographic signing operation.
        /// </summary>
        /// <param name="txHash">The hash of the transaction body.</param>
        /// <param name="metadataHash">The hash of the marshalled signature metadata.</param>
        /// <param name="keyPair">The key pair to use for signing.</param>
        void Sign(byte[] txHash, byte[] metadataHash, SignatureKeyPair keyPair);

        /// <summary>
        /// Marshals the signature object into its binary representation.
        /// </summary>
        /// <returns>A byte array containing the marshalled signature.</returns>
        byte[] MarshalBinary();

        /// <summary>
        /// Gets the underlying generated signature model object.
        /// </summary>
        /// <returns>The generated Signature object.</returns>
        Protocol.Generated.Signature GetModel(); // Using the base generated Signature type

        /// <summary>Encode only the signature metadata TLV (01,02,04,05,06).</summary>
        byte[] MarshalMetadata();
    }
}
