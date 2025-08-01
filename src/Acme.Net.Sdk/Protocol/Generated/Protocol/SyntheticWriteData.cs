using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a synthetic transaction for writing data to a data account.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class SyntheticWriteData : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "syntheticWriteData";

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
        /// Gets or sets the data entry.
        /// </summary>
        [JsonProperty("entry")]
        public IDataEntry? Entry { get; set; }

        /// <summary>
        /// Creates a new instance of SyntheticWriteData.
        /// </summary>
        public SyntheticWriteData()
        {
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.SyntheticWriteData);
            
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
            
            // Marshal entry as field 3
            if (Entry != null)
            {
                marshaller.WriteBytes(3, Entry.MarshalBinary());
            }
            
            return marshaller.GetBytes();
        }
    }
}