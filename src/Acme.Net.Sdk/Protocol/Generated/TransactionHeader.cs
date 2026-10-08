using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Commons.Codec;
using Acme.Net.Sdk.Commons.Codec.Binary;

namespace Acme.Net.Sdk.Protocol.Generated
{
    /// <summary>
    /// Represents a transaction header in the Accumulate protocol.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TransactionHeader : IMarshallable
    {
        /// <summary>
        /// Gets or sets the principal URL (origin of the transaction).
        /// </summary>
        [JsonProperty("principal")]
        public Url? Principal { get; set; }

        /// <summary>
        /// Gets or sets the initiator hash (for signature verification).
        /// </summary>
        [JsonProperty("initiator")]
        [JsonConverter(typeof(JsonConverters.HexConverter))]
        public byte[]? Initiator { get; set; }

        /// <summary>
        /// Gets or sets the memo (optional text note).
        /// </summary>
        [JsonProperty("memo")]
        public string? Memo { get; set; }

        /// <summary>
        /// Gets or sets the metadata (optional additional data).
        /// </summary>
        [JsonProperty("metadata")]
        [JsonConverter(typeof(JsonConverters.HexConverter))]
        public byte[]? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the expiration options (field 5).
        /// </summary>
        [JsonProperty("expire", NullValueHandling = NullValueHandling.Ignore)]
        public ExpireOptions? Expire { get; set; }

        /// <summary>
        /// Gets or sets the hold-until options (field 6).
        /// </summary>
        [JsonProperty("holdUntil", NullValueHandling = NullValueHandling.Ignore)]
        public HoldUntilOptions? HoldUntil { get; set; }

        /// <summary>
        /// Gets or sets additional authorities that must approve the transaction (field 7, repeated).
        /// </summary>
        [JsonProperty("authorities", NullValueHandling = NullValueHandling.Ignore)]
        public System.Collections.Generic.List<Url>? Authorities { get; set; }

        /// <summary>
        /// Gets or sets the hash lock (field 8, Accumulate 1.4.6.7).
        /// </summary>
        [JsonProperty("hashLock", NullValueHandling = NullValueHandling.Ignore)]
        public HashLockOptions? HashLock { get; set; }

        /// <summary>
        /// Sets the hash lock.
        /// </summary>
        public TransactionHeader WithHashLock(HashLockOptions hashLock)
        {
            HashLock = hashLock ?? throw new ArgumentNullException(nameof(hashLock));
            return this;
        }

        /// <summary>
        /// Sets the hash lock from its parts.
        /// </summary>
        public TransactionHeader WithHashLock(HashLockAlgorithm algorithm, byte[] hash, DateTimeOffset? expiration = null)
        {
            return WithHashLock(new HashLockOptions { HashAlgorithm = algorithm, Hash = hash, Expiration = expiration });
        }

        /// <summary>
        /// Sets the principal URL.
        /// </summary>
        /// <param name="principal">The principal URL.</param>
        /// <returns>This instance for method chaining.</returns>
        public TransactionHeader WithPrincipal(Url principal)
        {
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            return this;
        }

        /// <summary>
        /// Sets the principal URL from a string.
        /// </summary>
        /// <param name="principal">The principal URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">If principal is null or empty.</exception>
        public TransactionHeader WithPrincipal(string principal)
        {
            if (string.IsNullOrEmpty(principal))
                throw new ArgumentNullException(nameof(principal));
            
            return WithPrincipal(new Url(principal));
        }

        /// <summary>
        /// Sets the initiator hash.
        /// </summary>
        /// <param name="initiator">The initiator hash as a byte array.</param>
        /// <returns>This instance for method chaining.</returns>
        public TransactionHeader WithInitiator(byte[] initiator)
        {
            Initiator = initiator ?? throw new ArgumentNullException(nameof(initiator));
            return this;
        }

        /// <summary>
        /// Sets the initiator hash from a hexadecimal string.
        /// </summary>
        /// <param name="initiator">The initiator hash as a hexadecimal string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">If initiator is null or empty.</exception>
        /// <exception cref="DecoderException">If initiator is not a valid hexadecimal string.</exception>
        public TransactionHeader WithInitiator(string initiator)
        {
            if (string.IsNullOrEmpty(initiator))
                throw new ArgumentNullException(nameof(initiator));
            
            return WithInitiator(Hex.DecodeHex(initiator));
        }

        /// <summary>
        /// Sets the memo.
        /// </summary>
        /// <param name="memo">The memo.</param>
        /// <returns>This instance for method chaining.</returns>
        public TransactionHeader WithMemo(string memo)
        {
            Memo = memo;
            return this;
        }

        /// <summary>
        /// Sets the metadata.
        /// </summary>
        /// <param name="metadata">The metadata as a byte array.</param>
        /// <returns>This instance for method chaining.</returns>
        public TransactionHeader WithMetadata(byte[] metadata)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            return this;
        }

        /// <summary>
        /// Sets the metadata from a hexadecimal string.
        /// </summary>
        /// <param name="metadata">The metadata as a hexadecimal string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">If metadata is null or empty.</exception>
        /// <exception cref="DecoderException">If metadata is not a valid hexadecimal string.</exception>
        public TransactionHeader WithMetadata(string metadata)
        {
            if (string.IsNullOrEmpty(metadata))
                throw new ArgumentNullException(nameof(metadata));
            
            return WithMetadata(Hex.DecodeHex(metadata));
        }

        /// <summary>
        /// Marshals the transaction header into its binary representation.
        /// </summary>
        /// <returns>A byte array containing the marshalled transaction header.</returns>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            if (Principal != null)
            {
                marshaller.WriteUrl(1, Principal);
            }
            
            if (Initiator != null && Initiator.Length > 0)
            {
                marshaller.WriteHash(2, Initiator);
            }
            
            if (!string.IsNullOrEmpty(Memo))
            {
                marshaller.WriteString(3, Memo);
            }
            
            if (Metadata != null && Metadata.Length > 0)
            {
                marshaller.WriteBytes(4, Metadata);
            }
            
            if (Expire != null)
            {
                var b = Expire.MarshalBinary();
                if (b.Length > 0) marshaller.WriteBytes(5, b);
            }

            if (HoldUntil != null)
            {
                var b = HoldUntil.MarshalBinary();
                if (b.Length > 0) marshaller.WriteBytes(6, b);
            }

            if (Authorities != null)
            {
                foreach (var authority in Authorities)
                    marshaller.WriteUrl(7, authority);
            }

            if (HashLock != null)
            {
                var b = HashLock.MarshalBinary();
                if (b.Length > 0) marshaller.WriteBytes(8, b);
            }

            return marshaller.GetBytes();
        }
    }
} 