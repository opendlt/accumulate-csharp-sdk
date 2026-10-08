using System;
using System.Numerics;
using Newtonsoft.Json;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Synthetic transaction (type 0x37) that deposits tokens locked by a hash until the
    /// preimage is revealed. Binary: 1=Type, 2=SyntheticOrigin {1 Cause, 3 Initiator, 4 FeeRefund, 5 Index},
    /// 3=Token, 4=Amount, 5=Sender, 6=HashAlgorithm, 7=Hash, 8=Expiration, 9=IsIssuer.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class SyntheticLockedDeposit : ITransactionBody
    {
        [JsonProperty("type")]
        public string Type => "syntheticLockedDeposit";

        [JsonProperty("cause", NullValueHandling = NullValueHandling.Ignore)]
        public TxID? Cause { get; set; }

        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public Url? Source { get; set; }

        [JsonProperty("initiator", NullValueHandling = NullValueHandling.Ignore)]
        public Url? Initiator { get; set; }

        [JsonProperty("feeRefund", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? FeeRefund { get; set; }

        [JsonProperty("index", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? Index { get; set; }

        [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
        public Url? Token { get; set; }

        [JsonProperty("amount")]
        public BigInteger Amount { get; set; }

        [JsonProperty("sender", NullValueHandling = NullValueHandling.Ignore)]
        public Url? Sender { get; set; }

        [JsonProperty("hashAlgorithm", NullValueHandling = NullValueHandling.Ignore)]
        public HashLockAlgorithm HashAlgorithm { get; set; }

        [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(JsonConverters.HexConverter))]
        public byte[]? Hash { get; set; }

        [JsonProperty("expiration", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? Expiration { get; set; }

        [JsonProperty("isIssuer")]
        public bool IsIssuer { get; set; }

        public byte[] MarshalBinary()
        {
            using var m = new Marshaller();
            m.WriteUInt(1, TransactionTypeCode.SyntheticLockedDeposit);

            using (var om = new Marshaller())
            {
                if (Cause != null) om.WriteTxid(1, Cause);
                if (Initiator != null) om.WriteUrl(3, Initiator);
                if (FeeRefund.HasValue && FeeRefund.Value != 0) om.WriteUInt(4, new BigInteger(FeeRefund.Value));
                if (Index.HasValue && Index.Value != 0) om.WriteUInt(5, new BigInteger(Index.Value));
                m.WriteBytes(2, om.GetBytes());
            }

            if (Token != null) m.WriteUrl(3, Token);
            if (Amount.Sign != 0) m.WriteBytes(4, Amount.ToByteArray(isUnsigned: true, isBigEndian: true));
            if (Sender != null) m.WriteUrl(5, Sender);
            if (HashAlgorithm != HashLockAlgorithm.Unknown) m.WriteUInt(6, (int)HashAlgorithm);
            if (Hash != null && Hash.Length > 0) m.WriteBytes(7, Hash);
            if (Expiration.HasValue) m.WriteVarint(8, Expiration.Value.ToUnixTimeSeconds());
            if (IsIssuer) m.WriteBool(9, true);
            return m.GetBytes();
        }
    }
}
