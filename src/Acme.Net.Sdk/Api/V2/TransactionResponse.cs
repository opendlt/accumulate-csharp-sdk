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
        [JsonPropertyName("items")]
        public List<TransactionItem> Items { get; set; } = new List<TransactionItem>();

        /// <summary>
        /// Gets or sets the total number of items available.
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the starting index of this response.
        /// </summary>
        [JsonPropertyName("start")]
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the number of items in this response.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents a transaction item in a transaction response.
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
        /// Gets or sets the transaction's data.
        /// </summary>
        [JsonPropertyName("data")]
        public string? Data { get; set; }

        /// <summary>
        /// Gets or sets the amount transferred (for token transactions).
        /// </summary>
        [JsonPropertyName("amount")]
        public long? Amount { get; set; }

        /// <summary>
        /// Gets or sets the transaction's origin.
        /// </summary>
        [JsonPropertyName("from")]
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the transaction's destination.
        /// </summary>
        [JsonPropertyName("to")]
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets the transaction's timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the transaction's status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transaction's signatures.
        /// </summary>
        [JsonPropertyName("signatures")]
        public List<SignatureInfo>? Signatures { get; set; }
    }
} 