using System.Runtime.Serialization;

namespace Acme.Net.Sdk.Protocol.Generated // Mimicking Java package structure
{
    /// <summary>
    /// Defines different types of Accumulate signatures.
    ///
    /// <para>
    /// These values are consensus-critical wire values, NOT an arbitrary ordering. The type is
    /// marshalled as field 1 of the signature metadata, so it feeds the metadata hash, the
    /// transaction header initiator, and therefore the signing preimage. A wrong value produces a
    /// perfectly well-formed signature that the node rejects with "transaction is not signed" —
    /// there is nothing in the crypto or in any log to point at the enum.
    /// </para>
    /// <para>
    /// Authoritative source: Go core <c>protocol/enums_gen.go</c> (<c>SignatureTypeXxx</c>
    /// constants). Every member below is pinned to it by
    /// <c>test/Acme.Net.Sdk.Tests/Protocol/SignatureTypeValueTests.cs</c>; if you add or change a
    /// member, change that test in the same commit or it will fail.
    /// </para>
    /// <para>
    /// Values 4 and up were previously wrong here — invented in declaration order rather than taken
    /// from the protocol — which silently broke rsaSha256, ecdsaSha256, btc, eth, delegated,
    /// typedData and authority. It went unnoticed because the SDK had only ever been exercised on
    /// the Ed25519 path, whose values (1, 2, 3) happened to be right.
    /// </para>
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

        [EnumMember(Value = "receipt")]
        RECEIPT = 4,

        [EnumMember(Value = "partition")]
        PARTITION = 5,

        [EnumMember(Value = "set")]
        SET = 6,

        [EnumMember(Value = "remote")]
        REMOTE = 7,

        [EnumMember(Value = "btc")]
        BTC = 8,

        [EnumMember(Value = "btcLegacy")]
        BTC_LEGACY = 9,

        [EnumMember(Value = "eth")]
        ETH = 10,

        [EnumMember(Value = "delegated")]
        DELEGATED = 11,

        [EnumMember(Value = "internal")]
        INTERNAL = 12,

        [EnumMember(Value = "authority")]
        AUTHORITY = 13,

        /// <summary>
        /// RSA-SHA256, PKCS#1 v1.5 over the 32-byte signing preimage. The <c>publicKey</c> field is
        /// PKIX/SPKI DER, and a key page entry is <c>sha256(SPKI DER)</c>.
        /// Requires an executor at Vandenberg or later on the target network.
        /// </summary>
        [EnumMember(Value = "rsaSha256")]
        RSA_SHA256 = 14,

        /// <summary>
        /// ECDSA P-256 over the 32-byte signing preimage, ASN.1 DER encoded (NOT raw r||s). The
        /// <c>publicKey</c> field is PKIX/SPKI DER, and a key page entry is <c>sha256(SPKI DER)</c> —
        /// never a hash of the raw EC point.
        /// Requires an executor at Vandenberg or later on the target network.
        /// </summary>
        [EnumMember(Value = "ecdsaSha256")]
        ECDSA_SHA256 = 15,

        [EnumMember(Value = "typedData")]
        TYPED_DATA = 16,
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
