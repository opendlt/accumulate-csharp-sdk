using Acme.Net.Sdk.Protocol.Generated; // SignatureType

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// An <see cref="IAccumulateSigner"/> backed by a key this process cannot read: a smartcard, a
    /// Windows CNG key-storage provider, an HSM, a cloud KMS, or a remote signing service. You
    /// supply the public key and a delegate that signs 32 bytes; the SDK does the rest.
    ///
    /// <code>
    /// // A PIV / smartcard / TPM key reached through the Windows certificate store.
    /// using var ecdsa = cert.GetECDsaPrivateKey()!;
    /// var signer = new ExternalSigner(
    ///     SignatureType.ECDSA_SHA256,
    ///     ecdsa.ExportSubjectPublicKeyInfo(),                     // SPKI DER, not the raw point
    ///     preimage =&gt; ecdsa.SignHash(preimage, DSASignatureFormat.Rfc3279DerSequence));
    ///
    /// var smart = new SmartSigner(client.V3, signer, "acc://bank.acme/book/1");
    /// await smart.SignAndSubmitAsync(principal, body);
    /// </code>
    ///
    /// <para>
    /// <b>The encodings that matter</b>, because each one fails silently as
    /// "transaction is not signed" rather than as an error you can read:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>ECDSA P-256</b> — sign with <c>SignHash(preimage,
    ///   DSASignatureFormat.Rfc3279DerSequence)</c>. ASN.1 DER, never raw <c>r||s</c>, which is what
    ///   the default <c>SignHash</c> overload returns. Public key is
    ///   <c>ExportSubjectPublicKeyInfo()</c>.</item>
    ///   <item><b>RSA-SHA256</b> — <c>SignHash(preimage, HashAlgorithmName.SHA256,
    ///   RSASignaturePadding.Pkcs1)</c>. Public key is <c>ExportSubjectPublicKeyInfo()</c>.</item>
    ///   <item><b>Ed25519</b> — raw 32-byte public key. Prefer <see cref="SignatureKeyPair"/>
    ///   unless the key genuinely lives outside the process.</item>
    /// </list>
    /// <para>
    /// A key page entry for this signer is <c>sha256</c> of the same public-key bytes you pass
    /// here — for ECDSA and RSA that is a hash of the DER blob, never of the raw key material.
    /// </para>
    /// <para>
    /// ECDSA, RSA and typed-data signatures are gated on the node's executor version: a network
    /// older than Vandenberg rejects the signature type outright.
    /// </para>
    /// </summary>
    public sealed class ExternalSigner : IAccumulateSigner
    {
        private readonly byte[] _publicKey;
        private readonly Func<byte[], byte[]> _sign;

        /// <inheritdoc />
        public SignatureType SignatureType { get; }

        /// <param name="signatureType">
        /// The wire signature type. Must match what <paramref name="sign"/> actually produces —
        /// this value goes into the signing preimage, so a mismatch is unrecoverable and silent.
        /// </param>
        /// <param name="publicKey">
        /// The public key as it goes on the wire: raw 32 bytes for Ed25519, PKIX/SPKI DER for
        /// ECDSA and RSA. Copied on the way in and on the way out.
        /// </param>
        /// <param name="sign">
        /// Signs the 32-byte preimage. Called once per signature, on the calling thread. It must
        /// treat the input as a digest (<c>SignHash</c>, not <c>SignData</c>).
        /// </param>
        public ExternalSigner(SignatureType signatureType, byte[] publicKey, Func<byte[], byte[]> sign)
        {
            if (signatureType == SignatureType.UNKNOWN)
                throw new ArgumentException("Signature type must not be UNKNOWN.", nameof(signatureType));
            if (publicKey is null) throw new ArgumentNullException(nameof(publicKey));
            if (publicKey.Length == 0)
                throw new ArgumentException("Public key must not be empty.", nameof(publicKey));

            SignatureType = signatureType;
            _publicKey = (byte[])publicKey.Clone();
            _sign = sign ?? throw new ArgumentNullException(nameof(sign));
        }

        /// <inheritdoc />
        public byte[] GetPublicKey() => (byte[])_publicKey.Clone();

        /// <inheritdoc />
        public byte[] SignPreimage(byte[] preimage)
        {
            if (preimage is null) throw new ArgumentNullException(nameof(preimage));

            var signature = _sign(preimage);
            if (signature is null || signature.Length == 0)
            {
                // Caught here rather than at the node, which would only say "transaction is not signed".
                throw new InvalidOperationException(
                    $"The signing delegate for {SignatureType} returned no signature bytes.");
            }
            return signature;
        }
    }
}
