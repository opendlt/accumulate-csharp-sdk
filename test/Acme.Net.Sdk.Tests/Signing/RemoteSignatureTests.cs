using System.Security.Cryptography;
using System.Text;
using Acme.Net.Sdk.Codec;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.V3;
using NSec.Cryptography;
using Xunit;
using SignatureKeyPair = Acme.Net.Sdk.Signing.SignatureKeyPair;

namespace Acme.Net.Sdk.Tests.Signing
{
    /// <summary>
    /// Offline verification of the "sign pending" / remote-signing primitives that enable
    /// independent, asynchronous M-of-N signing (an authority adds its signature to a transaction
    /// it did NOT initiate, knowing only the transaction hash).
    ///
    /// These tests assert the crypto + envelope structure are correct without any network. The
    /// live pending→delivered round-trip on a real key page is exercised by
    /// examples/v3/Example15_SignPendingMultisig against Kermit.
    /// </summary>
    public class RemoteSignatureTests
    {
        private const string KeyPageUrl = "acc://verso-acme.acme/book/1";
        private static readonly SignatureAlgorithm Alg = SignatureAlgorithm.Ed25519;

        // A SmartSigner needs a client, but BuildSignature() never touches the network.
        private static SmartSigner OfflineSigner(SignatureKeyPair kp, string signerUrl) =>
            new SmartSigner(new AccumulateV3Client("http://localhost:1"), kp, signerUrl);

        private static SmartSigner.SignerMetadata BuildMeta(
            SignatureKeyPair kp, string signerUrl, int version, long ts, int vote, string? memo, byte[]? data)
        {
            var pub = kp.GetPublicKey();
            var metaHash = TransactionCodec.ComputeSignatureMetadataHash(
                pub, signerUrl, version, ts, signatureType: (int)kp.Type, vote: vote, memo: memo, data: data);
            return new SmartSigner.SignerMetadata(metaHash, ts, version, vote, memo, data, pub);
        }

        private static bool VerifyEd25519(byte[] pub, byte[] message, byte[] signature)
        {
            var publicKey = PublicKey.Import(Alg, pub, KeyBlobFormat.RawPublicKey);
            return Alg.Verify(publicKey, message, signature);
        }

        [Fact]
        public void TxBody_Remote_HasTypeAndHash()
        {
            var hashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("pending"))).ToLowerInvariant();
            var body = TxBody.Remote(hashHex);

            Assert.Equal("remote", body["type"]);
            Assert.Equal(hashHex, body["hash"]);
        }

        [Fact]
        public void TxBody_Remote_RejectsEmptyHash()
        {
            Assert.Throws<ArgumentException>(() => TxBody.Remote(""));
        }

        [Fact]
        public void RemoteSignature_BindsTransactionHash_AndVerifiesUnderSignerKey()
        {
            using var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var signer = OfflineSigner(kp, KeyPageUrl);

            // A transaction hash someone else initiated (we only know the hash).
            var txHash = SHA256.HashData(Encoding.UTF8.GetBytes("disposition approval KIT-00042"));
            var data = SHA256.HashData(Encoding.UTF8.GetBytes("compliance-evidence"));
            var meta = BuildMeta(kp, KeyPageUrl, version: 1, ts: 1_700_000_000_000_000L,
                vote: (int)VoteType.Accept, memo: "PHI handling verified", data: data);

            var sig = signer.BuildSignature(txHash, meta);

            // The signature references the EXACT transaction hash we were given.
            Assert.Equal(Convert.ToHexString(txHash).ToLowerInvariant(), sig["transactionHash"]);
            Assert.Equal(KeyPageUrl, sig["signer"]);
            Assert.Equal(1, sig["signerVersion"]);
            Assert.False(sig.ContainsKey("vote"));   // Accept == 0 is omitted, matching Go core
            Assert.Equal("PHI handling verified", sig["memo"]);

            // The signature verifies over SHA256(metadataHash || txHash) under the signer's public key.
            var preimage = TransactionCodec.CreateSigningPreimage(meta.MetadataHash, txHash);
            var sigBytes = Convert.FromHexString((string)sig["signature"]!);
            Assert.True(VerifyEd25519(kp.GetPublicKey(), preimage, sigBytes),
                "remote signature must verify under the signer's Ed25519 public key");
        }

        [Fact]
        public void IndependentCoSigners_SignTheSameHash_ProducingAggregatableSignatures()
        {
            // Two independent authorities (separate keys) sign the SAME pending transaction hash.
            using var keyA = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            using var keyB = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var signerA = OfflineSigner(keyA, KeyPageUrl);
            var signerB = OfflineSigner(keyB, KeyPageUrl);

            var txHash = SHA256.HashData(Encoding.UTF8.GetBytes("2-of-2 approval"));

            var metaA = BuildMeta(keyA, KeyPageUrl, 1, 1_700_000_000_000_000L, (int)VoteType.Accept, "QA pass", null);
            var metaB = BuildMeta(keyB, KeyPageUrl, 1, 1_700_000_000_000_001L, (int)VoteType.Accept, "compliance pass", null);

            var sigA = signerA.BuildSignature(txHash, metaA);
            var sigB = signerB.BuildSignature(txHash, metaB);

            // Both signatures reference the identical transaction hash (the multi-sig invariant)...
            Assert.Equal(sigA["transactionHash"], sigB["transactionHash"]);
            Assert.Equal(Convert.ToHexString(txHash).ToLowerInvariant(), sigA["transactionHash"]);

            // ...but are distinct signatures from distinct keys (the network aggregates them on the page).
            Assert.NotEqual((string)sigA["signature"]!, (string)sigB["signature"]!);
            Assert.NotEqual((string)sigA["publicKey"]!, (string)sigB["publicKey"]!);

            // Each verifies under its own key over its own metadata + the shared txHash.
            Assert.True(VerifyEd25519(keyA.GetPublicKey(),
                TransactionCodec.CreateSigningPreimage(metaA.MetadataHash, txHash), Convert.FromHexString((string)sigA["signature"]!)));
            Assert.True(VerifyEd25519(keyB.GetPublicKey(),
                TransactionCodec.CreateSigningPreimage(metaB.MetadataHash, txHash), Convert.FromHexString((string)sigB["signature"]!)));
        }
    }
}
