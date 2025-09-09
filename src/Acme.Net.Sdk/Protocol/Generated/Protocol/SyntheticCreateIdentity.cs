using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a synthetic transaction for creating an identity.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class SyntheticCreateIdentity : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "syntheticCreateIdentity";

        /// <summary>
        /// Gets or sets the cause transaction ID.
        /// </summary>
        [JsonProperty("cause")]
        public TxID? Cause { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        [JsonProperty("source")]
        public Url? Source { get; set; }

        /// <summary>
        /// Gets or sets the initiator URL.
        /// </summary>
        [JsonProperty("initiator")]
        public Url? Initiator { get; set; }

        /// <summary>
        /// Gets or sets the fee refund amount.
        /// </summary>
        [JsonProperty("feeRefund")]
        public ulong? FeeRefund { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        [JsonProperty("index")]
        public ulong? Index { get; set; }

        /// <summary>
        /// Gets or sets the accounts to create.
        /// </summary>
        [JsonProperty("accounts")]
        public List<IAccount> Accounts { get; set; } = new List<IAccount>();

        /// <summary>
        /// Creates a new instance of SyntheticCreateIdentity.
        /// </summary>
        public SyntheticCreateIdentity()
        {
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.SyntheticCreateIdentity);
            
            // Marshal synthetic transaction fields as nested field 2
            var syntheticMarshaller = new Marshaller();
            
            // Field 2,0: source
            if (Source != null)
            {
                syntheticMarshaller.WriteValue(0, Source);
            }
            
            // Field 2,1: cause
            if (Cause != null)
            {
                syntheticMarshaller.WriteValue(1, Cause);
            }
            
            // Field 2,3: initiator
            if (Initiator != null)
            {
                syntheticMarshaller.WriteValue(3, Initiator);
            }
            
            // Field 2,4: feeRefund
            if (FeeRefund.HasValue)
            {
                syntheticMarshaller.WriteUInt(4, FeeRefund.Value);
            }
            
            // Field 2,5: index
            if (Index.HasValue)
            {
                syntheticMarshaller.WriteUInt(5, Index.Value);
            }
            
            marshaller.WriteBytes(2, syntheticMarshaller.GetBytes());
            
            // Marshal accounts as field 3 (repeatable)
            if (Accounts != null && Accounts.Count > 0)
            {
                foreach (var account in Accounts)
                {
                    // Each account implements IMarshallable
                    marshaller.WriteValue(3, account);
                }
            }
            
            return marshaller.GetBytes();
        }
    }
}