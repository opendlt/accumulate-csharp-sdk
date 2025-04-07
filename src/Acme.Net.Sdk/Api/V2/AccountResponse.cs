using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents an account response from the Acme API.
    /// </summary>
    public class AccountResponse
    {
        /// <summary>
        /// Gets or sets the account type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the account URL.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the account's key book URL.
        /// </summary>
        [JsonPropertyName("keyBookUrl")]
        public string KeyBookUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the account's auth signatures.
        /// </summary>
        [JsonPropertyName("authSignatures")]
        public List<SignatureInfo> AuthSignatures { get; set; } = new List<SignatureInfo>();

        /// <summary>
        /// Gets or sets the account's ACME token balance.
        /// Only available for token accounts.
        /// </summary>
        [JsonPropertyName("balance")]
        public long? Balance { get; set; }

        /// <summary>
        /// Gets or sets the account's token URL.
        /// Only available for token accounts.
        /// </summary>
        [JsonPropertyName("tokenUrl")]
        public string? TokenUrl { get; set; }

        /// <summary>
        /// Gets or sets the account's credits balance.
        /// </summary>
        [JsonPropertyName("credits")]
        public long? Credits { get; set; }

        /// <summary>
        /// Gets or sets the account's data entries.
        /// Only available for data accounts.
        /// </summary>
        [JsonPropertyName("data")]
        public List<DataEntry>? Data { get; set; }
    }

    /// <summary>
    /// Represents signature information.
    /// </summary>
    public class SignatureInfo
    {
        /// <summary>
        /// Gets or sets the signature type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the signature itself.
        /// </summary>
        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the signer URL.
        /// </summary>
        [JsonPropertyName("signerUrl")]
        public string SignerUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a data entry in a data account.
    /// </summary>
    public class DataEntry
    {
        /// <summary>
        /// Gets or sets the entry's value.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entry's type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entry's transaction hash.
        /// </summary>
        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;
    }
} 