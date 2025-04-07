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
            
            return marshaller.GetBytes();
        }
    }
} 