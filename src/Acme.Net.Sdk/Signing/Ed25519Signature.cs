using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;
using NSec.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Implementation of Ed25519 signatures for Accumulate.
    /// </summary>
    public class Ed25519Signature : BaseSignature
    {
        /// <summary>
        /// Gets the signature type.
        /// </summary>
        public override SignatureType Type => SignatureType.ED25519;

        private static string Sha256Hex(byte[] data) =>
            Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

        /// <summary>
        /// Initializes a new instance of the <see cref="Ed25519Signature"/> class.
        /// </summary>
        public Ed25519Signature()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ed25519Signature"/> class with the specified URL and public key.
        /// </summary>
        /// <param name="url">The signer URL.</param>
        /// <param name="publicKey">The public key bytes.</param>
        public Ed25519Signature(Url url, byte[] publicKey)
        {
            SignerUrl = url ?? throw new ArgumentNullException(nameof(url));
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <summary>
        /// Performs the cryptographic signing operation using Ed25519.
        /// </summary>
        /// <param name="txHash">The hash of the transaction body.</param>
        /// <param name="metadataHash">The hash of the marshalled signature metadata.</param>
        /// <param name="keyPair">The key pair to use for signing.</param>
        /// <exception cref="ArgumentException">Thrown if the key pair type doesn't match the signature type.</exception>
        // Ed25519Signature.cs

        public override void Sign(byte[] txHash, byte[] metadataHash, SignatureKeyPair keyPair)
        {
            if (keyPair.Type != SignatureType.ED25519)
                throw new ArgumentException("Expected key pair of type ED25519", nameof(keyPair));
            if (txHash == null || txHash.Length == 0)
                throw new ArgumentException("Transaction hash cannot be null or empty", nameof(txHash));
            if (metadataHash == null || metadataHash.Length == 0 || metadataHash.Length != 32)
                throw new ArgumentException("metadataHash must be a 32-byte hash of the signature metadata", nameof(metadataHash));

            // Store txHash (this is what goes in tag 08)
            TransactionHash = (byte[])txHash.Clone();

            var alg = SignatureAlgorithm.Ed25519;
            Key key = keyPair.GetKey();

            if (PublicKey == null || PublicKey.Length == 0)
                PublicKey = keyPair.GetPublicKey();

            // const sigMdHash = sha256(encode(signature));
            // const hash      = sha256(sigMdHash || message.hash());
            // sign(hash)
            var concat = new byte[metadataHash.Length + txHash.Length];
            Buffer.BlockCopy(metadataHash, 0, concat, 0, metadataHash.Length);
            Buffer.BlockCopy(txHash,        0, concat, metadataHash.Length, txHash.Length);
            var toSign = SHA256.HashData(concat);

            SignatureBytes = alg.Sign(key, toSign);

            var pub = NSec.Cryptography.PublicKey.Import(alg, PublicKey, NSec.Cryptography.KeyBlobFormat.RawPublicKey);
            bool ok = alg.Verify(pub, toSign, SignatureBytes);

            Console.WriteLine($"[Ed25519Signature] toSign={Convert.ToHexString(toSign).ToLowerInvariant()}");
            Console.WriteLine($"[Ed25519Signature] txHash={Convert.ToHexString(TransactionHash).ToLowerInvariant()}");
            Console.WriteLine($"[Ed25519Signature] pubKey={Convert.ToHexString(PublicKey).ToLowerInvariant()}");
            Console.WriteLine($"[Ed25519Signature] sig   ={Convert.ToHexString(SignatureBytes).ToLowerInvariant()}");
            Console.WriteLine($"[Ed25519Signature] local verify: {(ok ? "OK" : "FAILED")}");

            if (!ok)
                throw new InvalidOperationException("Internal Ed25519 self-verify failed.");

            var sigBytes = this.MarshalBinary();
            Console.WriteLine($"[Ed25519Signature] TLV hex={Convert.ToHexString(sigBytes).ToLowerInvariant()}");
            Console.WriteLine($"[Ed25519Signature] TLV sha256={Convert.ToHexString(SHA256.HashData(sigBytes)).ToLowerInvariant()}");
        }

        /// <summary>
        /// Marshal only the signature metadata TLV (01,02,04,05,06).
        /// Excludes the signature bytes (03) and transactionHash (08).
        /// </summary>
        public override byte[] MarshalMetadata()
        {
            var m = new Marshaller();

            // 01: type (uvarint) — Ed25519 is 2 in the wire format
            m.WriteUInt(1, 2);

            // 02: publicKey (32 bytes)
            if (PublicKey == null || PublicKey.Length != 32)
                throw new InvalidOperationException("PublicKey must be 32 bytes for Ed25519.");
            m.WriteBytes(2, PublicKey);

            // 04: signer (URL)
            if (SignerUrl == null)
                throw new InvalidOperationException("SignerUrl is required.");
            m.WriteUrl(4, SignerUrl);

            // 05: signerVersion (uvarint)
            m.WriteUInt(5, (long)Version);

            // 06: timestamp (uvarint, microseconds)
            // BaseSignature.Timestamp should be ulong
            m.WriteUVarint(6, Timestamp);

            return m.GetBytes();
        }

        /// <summary>
        /// Marshals the signature object into its binary representation.
        /// </summary>
        /// <returns>A byte array containing the marshalled signature.</returns>
        // Ed25519Signature.cs (replace MarshalBinary with this)
        public override byte[] MarshalBinary()
        {
            var m = new Marshaller();

            m.WriteUInt(1, (int)SignatureType.ED25519);

            if (PublicKey == null || PublicKey.Length != 32)
                throw new InvalidOperationException("PublicKey must be 32 bytes for Ed25519.");
            m.WriteBytes(2, PublicKey);

            if (SignatureBytes == null || SignatureBytes.Length != 64)
                throw new InvalidOperationException("SignatureBytes must be 64 bytes for Ed25519.");
            m.WriteBytes(3, SignatureBytes);

            if (SignerUrl == null)
                throw new InvalidOperationException("SignerUrl is required.");
            m.WriteUrl(4, SignerUrl);

            // IMPORTANT: timestamp first
            m.WriteUVarint(6, (ulong)Timestamp);

            // then signerVersion
            m.WriteUInt(5, (long)Version);

            if (TransactionHash == null || TransactionHash.Length != 32)
                throw new InvalidOperationException("TransactionHash must be 32 bytes.");
            m.WriteHash(8, TransactionHash);

            return m.GetBytes();
        }

        /// <summary>
        /// Gets the underlying generated signature model object.
        /// </summary>
        /// <returns>The generated Signature object.</returns>
        public override Protocol.Generated.Signature GetModel()
        {
            // TODO: Implement when Protocol.Generated.Signature is available
            // For now, return a placeholder that will need to be updated
            return new Protocol.Generated.Signature
            {
                // Fill in required properties
                Type = (int)Type,
                PublicKey = PublicKey,
                SignerUrl = SignerUrl?.String(),
                Signature1 = SignatureBytes,
                TransactionHash = TransactionHash
            };
        }
    }
} 