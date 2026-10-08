using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated
{
    /// <summary>
    /// Hash algorithm used by a hash lock (Accumulate 1.4.6.7).
    /// </summary>
    [JsonConverter(typeof(HashLockAlgorithmConverter))]
    public enum HashLockAlgorithm
    {
        Unknown = 0,
        SHA256 = 1,
        SHA256D = 2,
        HASH160 = 3,
    }

    /// <summary>
    /// JSON converter for <see cref="HashLockAlgorithm"/>: writes "sha256" / "sha256d" / "hash160",
    /// reads names (case-insensitive) or numbers. Unrecognised names are rejected.
    /// </summary>
    public class HashLockAlgorithmConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(HashLockAlgorithm) || objectType == typeof(HashLockAlgorithm?);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return objectType == typeof(HashLockAlgorithm?) ? null : HashLockAlgorithm.Unknown;
            if (reader.TokenType == JsonToken.Integer)
            {
                var n = Convert.ToInt32(reader.Value);
                if (!Enum.IsDefined(typeof(HashLockAlgorithm), n))
                    throw new JsonSerializationException($"Unknown hash algorithm: {n}");
                return (HashLockAlgorithm)n;
            }
            if (reader.TokenType == JsonToken.String)
            {
                switch (((string)reader.Value!).ToLowerInvariant())
                {
                    case "unknown": return HashLockAlgorithm.Unknown;
                    case "sha256": return HashLockAlgorithm.SHA256;
                    case "sha256d": return HashLockAlgorithm.SHA256D;
                    case "hash160": return HashLockAlgorithm.HASH160;
                }
                throw new JsonSerializationException($"Unknown hash algorithm: {reader.Value}");
            }
            throw new JsonSerializationException($"Cannot read hash algorithm from {reader.TokenType}");
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            writer.WriteValue(((HashLockAlgorithm)value).ToString().ToLowerInvariant());
        }
    }

    /// <summary>
    /// Header field 8: locks the synthetic output until the preimage is revealed or the lock expires.
    /// Binary: 1=HashAlgorithm (enum), 2=Hash (bytes), 3=Expiration (time).
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class HashLockOptions
    {
        [JsonProperty("hashAlgorithm", NullValueHandling = NullValueHandling.Ignore)]
        public HashLockAlgorithm HashAlgorithm { get; set; }

        [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(JsonConverters.HexConverter))]
        public byte[]? Hash { get; set; }

        [JsonProperty("expiration", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? Expiration { get; set; }

        public byte[] MarshalBinary()
        {
            using var m = new Marshaller();
            if (HashAlgorithm != HashLockAlgorithm.Unknown) m.WriteUInt(1, (int)HashAlgorithm);
            if (Hash != null && Hash.Length > 0) m.WriteBytes(2, Hash);
            if (Expiration.HasValue) m.WriteVarint(3, Expiration.Value.ToUnixTimeSeconds());
            return m.GetBytes();
        }

        /// <summary>
        /// Throws if the node would reject this lock outright. Mirrors the node's checks: a known
        /// algorithm, a hash of the right length (32 bytes for SHA256/SHA256D, 20 for HASH160), and an
        /// expiration between 10 minutes and 30 days away.
        /// </summary>
        /// <param name="now">The current time (defaults to UTC now); injectable for tests.</param>
        /// <exception cref="ArgumentException">The lock would be rejected.</exception>
        public void ValidateForSubmit(DateTimeOffset? now = null)
        {
            var want = HashAlgorithm switch
            {
                HashLockAlgorithm.SHA256 or HashLockAlgorithm.SHA256D => 32,
                HashLockAlgorithm.HASH160 => 20,
                _ => throw new ArgumentException($"unsupported hash algorithm: {HashAlgorithm}"),
            };
            var len = Hash?.Length ?? 0;
            if (len != want)
                throw new ArgumentException($"hash must be {want} bytes for {HashAlgorithm}, got {len}");
            if (Expiration is null)
                throw new ArgumentException("expiration is required");
            var delta = Expiration.Value - (now ?? DateTimeOffset.UtcNow);
            if (delta < TimeSpan.FromMinutes(10))
                throw new ArgumentException("expiration must be at least 10 minutes in the future");
            if (delta > TimeSpan.FromDays(30))
                throw new ArgumentException("expiration must be at most 30 days in the future");
        }
    }

    /// <summary>Header field 5: the transaction expires at the given time.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ExpireOptions
    {
        [JsonProperty("atTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? AtTime { get; set; }

        public byte[] MarshalBinary()
        {
            using var m = new Marshaller();
            if (AtTime.HasValue) m.WriteVarint(1, AtTime.Value.ToUnixTimeSeconds());
            return m.GetBytes();
        }
    }

    /// <summary>Header field 6: hold the transaction as pending until the given minor block.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class HoldUntilOptions
    {
        [JsonProperty("minorBlock", NullValueHandling = NullValueHandling.Ignore)]
        public ulong MinorBlock { get; set; }

        public byte[] MarshalBinary()
        {
            using var m = new Marshaller();
            if (MinorBlock != 0) m.WriteUInt(1, new System.Numerics.BigInteger(MinorBlock));
            return m.GetBytes();
        }
    }
}
