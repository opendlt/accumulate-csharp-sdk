namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Immutable value object holding transaction header metadata.
    /// Matches Dart context.dart and Python convenience.py BuildContext.
    /// </summary>
    public class BuildContext
    {
        /// <summary>
        /// The principal (origin) URL for the transaction.
        /// </summary>
        public string Principal { get; }

        /// <summary>
        /// Timestamp in microseconds since Unix epoch.
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        /// Optional memo string for the transaction.
        /// </summary>
        public string? Memo { get; }

        /// <summary>
        /// Optional metadata bytes for the transaction.
        /// </summary>
        public byte[]? Metadata { get; }

        public BuildContext(string principal, long? timestamp = null, string? memo = null, byte[]? metadata = null)
        {
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Timestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
            Memo = memo;
            Metadata = metadata;
        }

        /// <summary>
        /// Creates a BuildContext with the current time as timestamp.
        /// </summary>
        public static BuildContext Now(string principal, string? memo = null, byte[]? metadata = null)
        {
            return new BuildContext(principal, memo: memo, metadata: metadata);
        }

        /// <summary>
        /// Returns the header fields as a dictionary for the transaction envelope.
        /// Null values are omitted.
        /// </summary>
        public Dictionary<string, object?> HeaderJson()
        {
            var header = new Dictionary<string, object?>
            {
                ["principal"] = Principal,
            };
            if (Memo != null) header["memo"] = Memo;
            if (Metadata != null) header["metadata"] = Convert.ToHexString(Metadata).ToLowerInvariant();
            return header;
        }

        /// <summary>
        /// Returns a new BuildContext with the specified memo.
        /// </summary>
        public BuildContext WithMemo(string memo)
        {
            return new BuildContext(Principal, Timestamp, memo, Metadata);
        }

        /// <summary>
        /// Returns a new BuildContext with the specified metadata.
        /// </summary>
        public BuildContext WithMetadata(byte[] metadata)
        {
            return new BuildContext(Principal, Timestamp, Memo, metadata);
        }
    }
}
