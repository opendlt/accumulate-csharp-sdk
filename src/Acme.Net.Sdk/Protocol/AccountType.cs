using System.Runtime.Serialization;

namespace Acme.Net.Sdk.Protocol.Generated // Mimicking Java package structure
{
    /// <summary>
    /// Defines different types of Accumulate accounts.
    /// Values match the authoritative Go core / Rust SDK definitions.
    /// </summary>
    public enum AccountType
    {
        [EnumMember(Value = "unknown")]
        UNKNOWN = 0,

        [EnumMember(Value = "identity")]
        IDENTITY = 1,

        [EnumMember(Value = "tokenIssuer")]
        TOKEN_ISSUER = 2,

        [EnumMember(Value = "tokenAccount")]
        TOKEN_ACCOUNT = 3,

        [EnumMember(Value = "liteTokenAccount")]
        LITE_TOKEN_ACCOUNT = 4,

        [EnumMember(Value = "keyPage")]
        KEY_PAGE = 5,

        [EnumMember(Value = "keyBook")]
        KEY_BOOK = 6,

        [EnumMember(Value = "dataAccount")]
        DATA_ACCOUNT = 7,

        [EnumMember(Value = "liteDataAccount")]
        LITE_DATA_ACCOUNT = 8,

        [EnumMember(Value = "unknownSigner")]
        UNKNOWN_SIGNER = 9,

        [EnumMember(Value = "liteIdentity")]
        LITE_IDENTITY = 10,

        [EnumMember(Value = "anchorLedger")]
        ANCHOR_LEDGER = 16,

        [EnumMember(Value = "blockLedger")]
        BLOCK_LEDGER = 17,

        [EnumMember(Value = "systemLedger")]
        SYSTEM_LEDGER = 18,

        [EnumMember(Value = "syntheticLedger")]
        SYNTHETIC_LEDGER = 19,
    }
}
