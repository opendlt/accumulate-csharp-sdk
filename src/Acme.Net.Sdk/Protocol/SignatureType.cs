using System.Runtime.Serialization;

namespace Acme.Net.Sdk.Protocol.Generated // Mimicking Java package structure
{
    /// <summary>
    /// Defines different types of Accumulate signatures.
    /// Values match the authoritative Go core / Rust SDK definitions.
    /// </summary>
    public enum SignatureType
    {
        [EnumMember(Value = "unknown")]
        UNKNOWN = 0,

        [EnumMember(Value = "legacyED25519")]
        LEGACY_ED25519 = 1,

        [EnumMember(Value = "ed25519")]
        ED25519 = 2,

        [EnumMember(Value = "rcd1")]
        RCD1 = 3,

        [EnumMember(Value = "btc")]
        BTC = 4,

        [EnumMember(Value = "btcLegacy")]
        BTC_LEGACY = 5,

        [EnumMember(Value = "eth")]
        ETH = 6,

        [EnumMember(Value = "delegated")]
        DELEGATED = 7,

        [EnumMember(Value = "internal")]
        INTERNAL = 8,

        [EnumMember(Value = "rsaSha256")]
        RSA_SHA256 = 9,

        [EnumMember(Value = "ecdsaSha256")]
        ECDSA_SHA256 = 10,

        [EnumMember(Value = "typedData")]
        TYPED_DATA = 11,

        [EnumMember(Value = "remote")]
        REMOTE = 12,

        [EnumMember(Value = "receipt")]
        RECEIPT = 13,

        [EnumMember(Value = "partition")]
        PARTITION = 14,

        [EnumMember(Value = "set")]
        SET = 15,

        [EnumMember(Value = "authority")]
        AUTHORITY = 16,
    }

    /// <summary>
    /// Extension methods for SignatureType enum.
    /// </summary>
    public static class SignatureTypeExtensions
    {
        private static readonly Dictionary<int, SignatureType> _byValue;
        private static readonly Dictionary<string, SignatureType> _byWireName;
        private static readonly Dictionary<SignatureType, string> _toWireName;

        static SignatureTypeExtensions()
        {
            _byValue = new Dictionary<int, SignatureType>();
            _byWireName = new Dictionary<string, SignatureType>(StringComparer.OrdinalIgnoreCase);
            _toWireName = new Dictionary<SignatureType, string>();

            foreach (SignatureType st in Enum.GetValues<SignatureType>())
            {
                _byValue[(int)st] = st;

                var memberInfo = typeof(SignatureType).GetMember(st.ToString());
                if (memberInfo.Length > 0)
                {
                    var attr = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
                    if (attr.Length > 0)
                    {
                        var wireName = ((EnumMemberAttribute)attr[0]).Value!;
                        _byWireName[wireName] = st;
                        _toWireName[st] = wireName;
                    }
                }
            }
        }

        /// <summary>
        /// Converts a byte value to a SignatureType.
        /// </summary>
        public static SignatureType FromValue(byte value)
        {
            if (_byValue.TryGetValue(value, out var st))
                return st;
            return SignatureType.UNKNOWN;
        }

        /// <summary>
        /// Converts an integer value to a SignatureType.
        /// </summary>
        public static SignatureType FromValue(int value)
        {
            if (_byValue.TryGetValue(value, out var st))
                return st;
            return SignatureType.UNKNOWN;
        }

        /// <summary>
        /// Converts a JSON wire name (e.g., "ed25519", "rcd1") to a SignatureType.
        /// </summary>
        public static SignatureType FromWireName(string wireName)
        {
            if (wireName != null && _byWireName.TryGetValue(wireName, out var st))
                return st;
            return SignatureType.UNKNOWN;
        }

        /// <summary>
        /// Gets the JSON wire name for a SignatureType (e.g., "ed25519", "rcd1").
        /// </summary>
        public static string GetWireName(this SignatureType signatureType)
        {
            if (_toWireName.TryGetValue(signatureType, out var name))
                return name;
            return "unknown";
        }
    }
}
