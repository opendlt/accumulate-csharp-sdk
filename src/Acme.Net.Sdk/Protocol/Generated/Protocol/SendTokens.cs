using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for sending tokens.
    /// JSON surface may include type/hash/meta, but TLV must ONLY contain recipients.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class SendTokens : ITransactionBody
    {
        // ----- JSON shape -----

        [JsonProperty("type")]
        public string Type => "sendTokens";

        [JsonProperty("to")]
        public List<TokenRecipient> Recipients { get; set; } = new();

        [JsonProperty("hash")]
        public string? Hash { get; set; }

        [JsonProperty("meta")]
        public JRaw? Meta { get; set; }

        // ----- Fluent helpers (JSON-only conveniences) -----

        public SendTokens WithHash(byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Hash = Convert.ToBase64String(value);
            return this;
        }

        public SendTokens WithHash(string value)
        {
            Hash = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        public SendTokens WithMeta(JRaw value)
        {
            Meta = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        public SendTokens WithMeta(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            Meta = new JRaw(value);
            return this;
        }

        public SendTokens WithRecipients(params TokenRecipient[] recipients)
        {
            if (recipients == null) throw new ArgumentNullException(nameof(recipients));
            Recipients.Clear();
            Recipients.AddRange(recipients);
            return this;
        }

        public SendTokens AddRecipient(TokenRecipient recipient)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            Recipients.Add(recipient);
            return this;
        }

        public SendTokens AddRecipient(Url url, ulong amount)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            Recipients.Add(new TokenRecipient { Url = url, Amount = new BigInteger(amount) });
            return this;
        }

        public SendTokens AddRecipient(string url, ulong amount)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return AddRecipient(new Url(url), amount);
        }

        // ----- TLV encoding (must match Go exactly) -----

        /// <summary>
        /// Marshal ONLY the recipients as TLV:
        ///   Body tag 1 (repeated) -> TokenRecipient TLV
        /// No type/hash/meta in TLV.
        /// </summary>
        public byte[] MarshalBinary()
        {
            var m = new Marshaller();

            // tag 01 -> type = sendTokens (3)
            // Use whatever enum/constant you have for "sendTokens"
            // Examples:
            // m.WriteUInt(1, (int)TransactionTypeCode.SendTokens);
            // or
            // m.WriteUInt(1, 3);
            m.WriteUInt(1, 3);

            // tag 04 -> each recipient as its own TLV blob (repeated field)
            if (Recipients != null && Recipients.Count > 0)
            {
                foreach (var r in Recipients)
                {
                    // r.MarshalBinary() returns the inner TLV:
                    //   01 url (string)
                    //   02 amount (bytes big-endian minimal)
                    // We must wrap that under tag 04 at the body level:
                    m.WriteBytes(4, r.MarshalBinary());
                }
            }

            return m.GetBytes();
        }
    }

    /// <summary>
    /// TokenRecipient TLV:
    ///   tag 1 -> Url (URL)
    ///   tag 2 -> Amount (unsigned big-endian, minimal)
    /// </summary>
    public class TokenRecipient : IMarshallable
    {
        [JsonProperty("url")]
        public Url Url { get; set; } = new Url("acc://example.acme");

        [JsonProperty("amount")]
        public BigInteger Amount { get; set; }

        public byte[] MarshalBinary()
        {
            var m = new Marshaller();

            // tag 1 → URL (must be WriteUrl)
            m.WriteUrl(1, Url);

            // tag 2 → amount (unsigned, big-endian, minimal)
            if (Amount.Sign < 0)
                throw new ArgumentOutOfRangeException(nameof(Amount), "Amount must be unsigned");

            var amountBytes = Amount.Sign == 0
                ? Array.Empty<byte>()
                : Amount.ToByteArray(isUnsigned: true, isBigEndian: true);

            m.WriteBytes(2, amountBytes);

            return m.GetBytes();
        }
    }
}
