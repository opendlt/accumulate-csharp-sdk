using System.Security.Cryptography;
using Acme.Net.Sdk.Codec;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.V3;
using Xunit;

namespace Acme.Net.Sdk.Tests.Signing
{
    /// <summary>
    /// The external-signer hook: a key this process cannot read can sign, and the SDK carries its
    /// bytes through unchanged.
    ///
    /// <para>
    /// A SmartSigner needs a client, but BuildSignature() never touches the network — same approach
    /// as <see cref="RemoteSignatureTests"/>.
    /// </para>
    /// </summary>
    public class ExternalSignerTests
    {
        private const string SignerUrl = "acc://bank.acme/book/1";

        private static SmartSigner.SignerMetadata Meta(byte[] publicKey, int signatureType)
        {
            const long timestamp = 1_700_000_000_000_000L;
            const int version = 3;
            var metadataHash = TransactionCodec.ComputeSignatureMetadataHash(
                publicKey, SignerUrl, version, timestamp, signatureType: signatureType);
            return new SmartSigner.SignerMetadata(metadataHash, timestamp, version, 0, null, null, publicKey);
        }

        private static SmartSigner SignerFor(IAccumulateSigner signer)
            => new(new AccumulateV3Client("http://localhost:1"), signer, SignerUrl);

        /// <summary>
        /// The whole point: a real P-256 key signs, and the signature the SDK emits verifies against
        /// the preimage the protocol defines. Nothing about the key is available to the SDK except
        /// its public bytes and the delegate.
        /// </summary>
        [Fact]
        public void EcdsaKeyOutsideTheSdkProducesAVerifiableSignature()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var spki = ecdsa.ExportSubjectPublicKeyInfo();

            byte[]? sawPreimage = null;
            var external = new ExternalSigner(SignatureType.ECDSA_SHA256, spki, preimage =>
            {
                sawPreimage = preimage;
                // SignHash, not SignData: the preimage is already a digest. DER, not raw r||s.
                return ecdsa.SignHash(preimage, DSASignatureFormat.Rfc3279DerSequence);
            });

            var txHash = SHA256.HashData(new byte[] { 1, 2, 3 });
            var meta = Meta(spki, (int)SignatureType.ECDSA_SHA256);
            var signature = SignerFor(external).BuildSignature(txHash, meta);

            Assert.Equal("ecdsaSha256", signature["type"]);
            Assert.Equal(Convert.ToHexString(spki).ToLowerInvariant(), signature["publicKey"]);
            Assert.Equal(SignerUrl, signature["signer"]);

            // The delegate was handed exactly sha256(metadataHash || txHash).
            Assert.NotNull(sawPreimage);
            Assert.Equal(TransactionCodec.CreateSigningPreimage(meta.MetadataHash, txHash), sawPreimage);

            var sigBytes = Convert.FromHexString((string)signature["signature"]!);
            Assert.True(
                ecdsa.VerifyHash(sawPreimage!, sigBytes, DSASignatureFormat.Rfc3279DerSequence),
                "the signature the SDK emitted does not verify over the preimage it signed");
        }

        /// <summary>
        /// RSA takes the same path — only the padding and the type differ.
        /// </summary>
        [Fact]
        public void RsaKeyOutsideTheSdkProducesAVerifiableSignature()
        {
            using var rsa = RSA.Create(2048);
            var spki = rsa.ExportSubjectPublicKeyInfo();

            var external = new ExternalSigner(SignatureType.RSA_SHA256, spki,
                preimage => rsa.SignHash(preimage, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

            var txHash = SHA256.HashData(new byte[] { 4, 5, 6 });
            var meta = Meta(spki, (int)SignatureType.RSA_SHA256);
            var signature = SignerFor(external).BuildSignature(txHash, meta);

            Assert.Equal("rsaSha256", signature["type"]);
            var preimage = TransactionCodec.CreateSigningPreimage(meta.MetadataHash, txHash);
            var sigBytes = Convert.FromHexString((string)signature["signature"]!);
            Assert.True(
                rsa.VerifyHash(preimage, sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                "the signature the SDK emitted does not verify over the preimage it signed");
        }

        /// <summary>
        /// The signature type reaches the metadata hash, which is why getting it wrong is
        /// unrecoverable: two otherwise identical signers that disagree on the type sign different
        /// preimages. This is the failure the enum bug caused.
        /// </summary>
        [Fact]
        public void TheSignatureTypeChangesThePreimage()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var spki = ecdsa.ExportSubjectPublicKeyInfo();

            var asEcdsa = Meta(spki, (int)SignatureType.ECDSA_SHA256);
            var asRsa = Meta(spki, (int)SignatureType.RSA_SHA256);
            var wrongOldValue = Meta(spki, 10); // what ECDSA_SHA256 used to be

            Assert.NotEqual(asEcdsa.MetadataHash, asRsa.MetadataHash);
            Assert.NotEqual(asEcdsa.MetadataHash, wrongOldValue.MetadataHash);
        }

        /// <summary>
        /// An in-process Ed25519 key pair is an IAccumulateSigner too, and routing it through the
        /// new constructor must produce byte-identical output to the old one.
        /// </summary>
        [Fact]
        public void TheEd25519PathIsUnchangedByTheHook()
        {
            var seed = new byte[32];
            for (int i = 0; i < seed.Length; i++) seed[i] = (byte)(i + 1);
            Assert.True(SignatureKeyPair.TryImportFromSecretKeyBytes(seed, SignatureType.ED25519, out var kp));
            using var keypair = kp;

            var txHash = SHA256.HashData(new byte[] { 7, 8, 9 });
            var meta = Meta(keypair.GetPublicKey(), (int)SignatureType.ED25519);

            var viaKeyPairCtor = new SmartSigner(new AccumulateV3Client("http://localhost:1"), keypair, SignerUrl)
                .BuildSignature(txHash, meta);
            var viaInterfaceCtor = SignerFor(keypair).BuildSignature(txHash, meta);

            Assert.Equal("ed25519", viaKeyPairCtor["type"]);
            Assert.Equal(viaKeyPairCtor["signature"], viaInterfaceCtor["signature"]);
            Assert.Equal(viaKeyPairCtor["publicKey"], viaInterfaceCtor["publicKey"]);
        }

        [Fact]
        public void RefusesAnUnusableConfiguration()
        {
            var key = new byte[] { 1 };
            Assert.Throws<ArgumentException>(() =>
                new ExternalSigner(SignatureType.UNKNOWN, key, _ => key));
            Assert.Throws<ArgumentException>(() =>
                new ExternalSigner(SignatureType.ECDSA_SHA256, Array.Empty<byte>(), _ => key));
            Assert.Throws<ArgumentNullException>(() =>
                new ExternalSigner(SignatureType.ECDSA_SHA256, key, null!));

            // A delegate that returns nothing is caught here rather than at the node, which would
            // only ever say "transaction is not signed".
            var empty = new ExternalSigner(SignatureType.ECDSA_SHA256, key, _ => Array.Empty<byte>());
            Assert.Throws<InvalidOperationException>(() => empty.SignPreimage(new byte[32]));
        }

        /// <summary>
        /// The public key is copied in both directions, so a caller cannot mutate what the signer
        /// will put on the wire.
        /// </summary>
        [Fact]
        public void ThePublicKeyIsCopiedNotAliased()
        {
            var key = new byte[] { 1, 2, 3, 4 };
            var signer = new ExternalSigner(SignatureType.ECDSA_SHA256, key, _ => new byte[] { 9 });

            key[0] = 0xFF;
            Assert.Equal(1, signer.GetPublicKey()[0]);

            var handedOut = signer.GetPublicKey();
            handedOut[0] = 0xFF;
            Assert.Equal(1, signer.GetPublicKey()[0]);
        }
    }
}
