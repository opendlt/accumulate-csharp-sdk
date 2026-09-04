using System.Security.Cryptography;
using Acme.Net.Sdk.Protocol.Generated; // SignatureType

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// The one thing a signer must be able to do: turn the 32-byte signing preimage into signature
    /// bytes, and say which key and which signature type produced them.
    ///
    /// <para>
    /// This exists so the private key does not have to be inside the process. A smartcard, a
    /// Windows CNG key-storage provider, an HSM, a remote signing service and a cloud KMS all
    /// expose "sign these bytes" and none of them will hand over key material —
    /// <see cref="SignatureKeyPair"/> cannot represent any of them, because it is built around a
    /// raw 32-byte Ed25519 seed.
    /// </para>
    /// <para>
    /// Everything above the signature — metadata marshalling, the transaction hash, the preimage —
    /// is identical for every signature type, so an implementation only has to get its own two
    /// encodings right. See <see cref="ExternalSigner"/> for the encodings each type expects.
    /// </para>
    /// </summary>
    public interface IAccumulateSigner
    {
        /// <summary>
        /// The signature type. Marshalled as field 1 of the signature metadata, so it is part of
        /// the signing preimage: a value that does not match the bytes produced by
        /// <see cref="SignPreimage"/> yields "transaction is not signed" from the node.
        /// </summary>
        SignatureType SignatureType { get; }

        /// <summary>
        /// The public key exactly as it goes on the wire in the signature's <c>publicKey</c> field,
        /// and exactly as a key page entry hashes it (<c>sha256</c> of these bytes).
        /// Raw 32 bytes for Ed25519; PKIX/SPKI DER for ECDSA and RSA.
        /// </summary>
        byte[] GetPublicKey();

        /// <summary>
        /// Sign the 32-byte signing preimage, which is <c>sha256(metadataHash || txHash)</c>.
        ///
        /// <para>
        /// The preimage is ALREADY a digest. Implementations must sign it as a hash — .NET's
        /// <c>SignHash</c>, not <c>SignData</c>. Hashing it a second time produces a structurally
        /// valid signature that no verifier will accept.
        /// </para>
        /// </summary>
        byte[] SignPreimage(byte[] preimage);
    }

    /// <summary>
    /// Helpers every <see cref="IAccumulateSigner"/> gets for free.
    /// </summary>
    public static class AccumulateSignerExtensions
    {
        /// <summary>
        /// This signer's key page entry: <c>sha256</c> of the public key exactly as it goes on the
        /// wire. Pass it to <see cref="SmartSigner.AddKeyAsync(byte[])"/>, or use the
        /// <see cref="SmartSigner.AddKeyAsync(IAccumulateSigner)"/> overload, which calls this for you.
        ///
        /// <para>
        /// Defined in terms of <see cref="IAccumulateSigner.GetPublicKey"/>, so it is right for
        /// every signature type without the caller having to know which encoding applies: raw 32
        /// bytes for Ed25519, PKIX/SPKI DER for ECDSA and RSA. Hashing the wrong bytes — the raw EC
        /// point rather than the DER, the usual mistake — produces a page entry that simply never
        /// matches the signature, and the node reports nothing more useful than a signature it will
        /// not accept.
        /// </para>
        /// </summary>
        public static byte[] GetPublicKeyHash(this IAccumulateSigner signer)
        {
            if (signer is null) throw new ArgumentNullException(nameof(signer));
            return SHA256.HashData(signer.GetPublicKey());
        }
    }
}
