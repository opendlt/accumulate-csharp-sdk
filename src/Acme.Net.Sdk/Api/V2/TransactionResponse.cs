using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Acme.Net.Sdk.Api.V2
{
    /// <summary>
    /// Represents a transaction response from the Acme API.
    /// </summary>
    public class TransactionResponse
    {
        /// <summary>
        /// Gets or sets the list of transactions.
        /// </summary>
        [JsonPropertyName("transactions")]
        public List<TransactionItem> Transactions { get; set; } = new List<TransactionItem>();

        /// <summary>
        /// Gets or sets the total number of transactions.
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the start index for pagination.
        /// </summary>
        [JsonPropertyName("start")]
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the count of transactions returned.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents a transaction item in the transaction response.
    /// </summary>
    public class TransactionItem
    {
        /// <summary>
        /// Gets or sets the transaction ID.
        /// </summary>
        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transaction type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transaction data.
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transaction timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the transaction status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the principal URL.
        /// </summary>
        [JsonPropertyName("principal")]
        public string Principal { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initiator URL.
        /// </summary>
        [JsonPropertyName("initiator")]
        public string Initiator { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transaction memo.
        /// </summary>
        [JsonPropertyName("memo")]
        public string? Memo { get; set; }

        /// <summary>
        /// Gets or sets the transaction metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the signatures.
        /// </summary>
        [JsonPropertyName("signatures")]
        public List<SignatureInfo> Signatures { get; set; } = new List<SignatureInfo>();

        /// <summary>
        /// Gets or sets the transaction result.
        /// </summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the transaction error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
} 