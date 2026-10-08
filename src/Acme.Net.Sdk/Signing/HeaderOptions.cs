namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Optional transaction-header fields beyond principal / initiator / memo / metadata.
    /// Each one is part of the binary header, so it changes the transaction hash: the same
    /// options must be on the header every co-signer hashes (they travel with the header
    /// dictionary in the envelope). Unset fields are omitted, and a header without any of
    /// them encodes exactly as it always did.
    /// </summary>
    public sealed class HeaderOptions
    {
        /// <summary>Header field 5: <c>{ "atTime": ISO-8601 }</c> - expire the transaction as pending at this time.</summary>
        public Dictionary<string, object?>? Expire { get; init; }

        /// <summary>Header field 6: <c>{ "minorBlock": N }</c> - hold the transaction as pending until this minor block.</summary>
        public Dictionary<string, object?>? HoldUntil { get; init; }

        /// <summary>Header field 7: additional authorities (URLs) that must approve the transaction.</summary>
        public IReadOnlyList<string>? Authorities { get; init; }

        /// <summary>Header field 8: hash lock (HTLC). Build with <c>TxBody.HashLock(...)</c>.</summary>
        public Dictionary<string, object?>? HashLock { get; init; }

        internal void ApplyTo(Dictionary<string, object?> header)
        {
            if (Expire != null) header["expire"] = Expire;
            if (HoldUntil != null) header["holdUntil"] = HoldUntil;
            if (Authorities is { Count: > 0 }) header["authorities"] = Authorities.ToList();
            if (HashLock != null) header["hashLock"] = HashLock;
        }
    }
}
