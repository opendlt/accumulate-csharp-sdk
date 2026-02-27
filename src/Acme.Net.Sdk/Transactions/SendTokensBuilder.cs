using System;
using System.Collections.Generic;
using System.Numerics;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for sendTokens transactions.
    /// </summary>
    public class SendTokensBuilder : TransactionBuilder
    {
        private readonly List<TokenRecipient> _recipients = new();
        private string? _hash; // optional JSON-only
        private string? _meta; // optional JSON-only

        public SendTokensBuilder(ApiClient client) : base(client) { }

        public SendTokensBuilder AddRecipient(Url url, ulong amount)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            _recipients.Add(new TokenRecipient { Url = url, Amount = new BigInteger(amount) });
            return this;
        }

        public SendTokensBuilder AddRecipient(string url, ulong amount)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return AddRecipient(new Url(url), amount);
        }

        // Optional JSON conveniences (do not affect TLV)
        public SendTokensBuilder WithHash(string hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
            return this;
        }

        public SendTokensBuilder WithMeta(string meta)
        {
            _meta = meta ?? throw new ArgumentNullException(nameof(meta));
            return this;
        }

        protected override void Validate()
        {
            base.Validate();
            if (_recipients.Count == 0)
                throw new InvalidOperationException("At least one recipient must be specified");
        }

        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var body = new SendTokens();
            foreach (var r in _recipients)
                body.AddRecipient(r);

            // Keep JSON fields if you use them elsewhere; TLV remains minimal
            if (!string.IsNullOrEmpty(_hash))
                body.WithHash(_hash);
            if (!string.IsNullOrEmpty(_meta))
                body.WithMeta(_meta);

            return body;
        }
    }
}
