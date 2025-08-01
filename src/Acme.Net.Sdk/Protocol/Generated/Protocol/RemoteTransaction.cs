using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a remote transaction used to sign a remote transaction (SignPending).
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class RemoteTransaction : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "remote";

        /// <summary>
        /// Gets or sets the hash of the remote transaction to sign.
        /// </summary>
        [JsonProperty("hash")]
        public byte[]? Hash { get; set; }

        /// <summary>
        /// Gets or sets the cause transaction.
        /// </summary>
        [JsonProperty("cause")]
        public TxID? Cause { get; set; }

        /// <summary>
        /// Creates a new instance of RemoteTransaction.
        /// </summary>
        public RemoteTransaction()
        {
        }

        /// <summary>
        /// Sets the hash.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns>The current instance for method chaining.</returns>
        public RemoteTransaction WithHash(byte[] hash)
        {
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
            return this;
        }

        /// <summary>
        /// Sets the cause.
        /// </summary>
        /// <param name="cause">The cause transaction ID.</param>
        /// <returns>The current instance for method chaining.</returns>
        public RemoteTransaction WithCause(TxID cause)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause));
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.Remote);
            
            // Note: The JavaScript SDK doesn't show the field structure for Remote
            // This is a placeholder implementation
            
            // Marshal hash if present
            if (Hash != null)
            {
                marshaller.WriteBytes(2, Hash);
            }
            
            // Marshal cause if present
            if (Cause != null)
            {
                marshaller.WriteValue(3, Cause);
            }
            
            return marshaller.GetBytes();
        }
    }
}