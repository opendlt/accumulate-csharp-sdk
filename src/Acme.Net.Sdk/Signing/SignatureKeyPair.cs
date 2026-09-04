using System;
using System.Diagnostics.CodeAnalysis;
using NSec.Cryptography;
using Acme.Net.Sdk.Protocol.Generated; // SignatureType

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Canonical key pair for Accumulate signing. Always derives the public key
    /// and the signing key from the EXACT SAME 32-byte Ed25519 seed via NSec.
    ///
    /// <para>
    /// This holds key material in the process, so it can only ever be Ed25519. For a key that
    /// lives in a smartcard, a CNG provider, an HSM or a KMS, use <see cref="ExternalSigner"/> —
    /// both are <see cref="IAccumulateSigner"/> and <see cref="SmartSigner"/> takes either.
    /// </para>
    /// </summary>
    public sealed class SignatureKeyPair : IDisposable, IAccumulateSigner
    {
        private readonly byte[] _seed32;   // immutable 32-byte Ed25519 seed
        private readonly byte[] _pub32;    // derived from _seed32 once
        private Key? _key;                 // NSec Key from _seed32 (lazy); disposable

        public SignatureType Type { get; }

        private static readonly SignatureAlgorithm Alg = SignatureAlgorithm.Ed25519;

        private SignatureKeyPair(byte[] seed32)
        {
            Type    = SignatureType.ED25519;
            _seed32 = (byte[])seed32.Clone();

            // Derive pub once from the seed (guarantees same derivation as signing)
            using var k = Key.Import(Alg, _seed32, KeyBlobFormat.RawPrivateKey,
                new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

            _pub32 = k.Export(KeyBlobFormat.RawPublicKey);
        }

        /// <summary>
        /// Accepts a 32-byte Ed25519 seed. If 64 bytes are provided (seed||pub),
        /// we take the first 32 as seed (common format from some libs).
        /// </summary>
        public static bool TryImportFromSecretKeyBytes(ReadOnlySpan<byte> secret, SignatureType type,
            [MaybeNullWhen(false)] out SignatureKeyPair keyPair)
        {
            keyPair = null;
            if (type != SignatureType.ED25519) return false;

            byte[] seed32;
            if (secret.Length == 32)
            {
                seed32 = secret.ToArray();
            }
            else if (secret.Length == 64)
            {
                // seed || pub (ignore trailing 32 bytes)
                seed32 = secret.Slice(0, 32).ToArray();
            }
            else
            {
                return false;
            }

            try
            {
                keyPair = new SignatureKeyPair(seed32);

                // Debug logging (safe): show derived public key so you can compare with on-chain key page
                Console.WriteLine("[SignatureKeyPair] Imported Ed25519 seed (32 bytes).");
                Console.WriteLine("[SignatureKeyPair] Derived pubkey (hex): " + Convert.ToHexString(keyPair._pub32).ToLowerInvariant());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SignatureKeyPair] Import failed: " + ex.Message);
                keyPair = null;
                return false;
            }
        }

        /// <summary>
        /// Lazily create (or reuse) the NSec Key imported from the stored seed.
        /// </summary>
        internal Key GetKey()
        {
            if (_key is not null) return _key;

            _key = Key.Import(Alg, _seed32, KeyBlobFormat.RawPrivateKey,
                new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

            return _key;
        }

        /// <summary>
        /// Return a copy of the raw 32-byte public key.
        /// </summary>
        public byte[] GetPublicKey() => (byte[])_pub32.Clone();

        /// <summary>
        /// <see cref="IAccumulateSigner.SignatureType"/>. Implemented explicitly so the existing
        /// <see cref="Type"/> property stays the one callers use.
        /// </summary>
        SignatureType IAccumulateSigner.SignatureType => Type;

        /// <summary>
        /// Sign the 32-byte preimage with Ed25519. NSec signs the bytes it is given, which is
        /// correct here: the preimage is already sha256(metadataHash || txHash), and Ed25519's own
        /// hashing is part of the algorithm rather than a second pass over the message.
        /// </summary>
        byte[] IAccumulateSigner.SignPreimage(byte[] preimage)
            => Alg.Sign(GetKey(), preimage);

        /// <summary>
        /// (Optional) Raw 32-byte seed export. Use sparingly.
        /// </summary>
        public byte[] GetPrivateKeyBytes() => (byte[])_seed32.Clone();

        public void Dispose()
        {
            _key?.Dispose();
            _key = null;
        }
    }
}
