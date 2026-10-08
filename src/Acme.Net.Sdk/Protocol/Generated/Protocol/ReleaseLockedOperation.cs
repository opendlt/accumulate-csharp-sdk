using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Releases a locked (hash-locked) deposit by revealing the hash preimage.
    /// Transaction type 0x18. Binary: 1=Type, 2=LockedTxID (txid), 3=Preimage (bytes).
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class ReleaseLockedOperation : ITransactionBody
    {
        [JsonProperty("type")]
        public string Type => "releaseLockedOperation";

        /// <summary>The ID of the SyntheticLockedDeposit to release.</summary>
        [JsonProperty("lockedTxID", NullValueHandling = NullValueHandling.Ignore)]
        public TxID? LockedTxID { get; set; }

        /// <summary>The preimage whose hash matches the lock.</summary>
        [JsonProperty("preimage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(JsonConverters.HexConverter))]
        public byte[]? Preimage { get; set; }

        public ReleaseLockedOperation WithLockedTxID(TxID id) { LockedTxID = id ?? throw new ArgumentNullException(nameof(id)); return this; }
        public ReleaseLockedOperation WithLockedTxID(string id) => WithLockedTxID(new TxID(id));
        public ReleaseLockedOperation WithPreimage(byte[] preimage) { Preimage = preimage ?? throw new ArgumentNullException(nameof(preimage)); return this; }

        public byte[] MarshalBinary()
        {
            using var m = new Marshaller();
            m.WriteUInt(1, TransactionTypeCode.ReleaseLockedOperation);
            if (LockedTxID != null) m.WriteTxid(2, LockedTxID);
            if (Preimage != null && Preimage.Length > 0) m.WriteBytes(3, Preimage);
            return m.GetBytes();
        }
    }
}
