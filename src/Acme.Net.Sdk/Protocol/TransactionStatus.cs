using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents the status of a transaction on the Accumulate network.
    /// </summary>
    public class TransactionStatus
    {
        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        [JsonProperty("code")]
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the transaction ID.
        /// </summary>
        [JsonProperty("txid")]
        [JsonPropertyName("txid")]
        public string? TxId { get; set; }

        /// <summary>
        /// Gets or sets whether the transaction was delivered.
        /// </summary>
        [JsonProperty("delivered")]
        [JsonPropertyName("delivered")]
        public bool Delivered { get; set; }

        /// <summary>
        /// Gets or sets whether the transaction is pending.
        /// </summary>
        [JsonProperty("pending")]
        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        /// <summary>
        /// Gets or sets the result of the transaction.
        /// </summary>
        [JsonProperty("result")]
        [JsonPropertyName("result")]
        public object? Result { get; set; }

        /// <summary>
        /// Determines whether the transaction completed successfully.
        /// </summary>
        /// <returns>True if the transaction succeeded, false otherwise.</returns>
        public bool IsSuccess()
        {
            return (Code == 0 || Code == 200) && string.IsNullOrEmpty(Error);
        }

        /// <summary>
        /// Determines whether the transaction has an error.
        /// </summary>
        /// <returns>True if the transaction has an error, false otherwise.</returns>
        public bool HasError()
        {
            return !IsSuccess() || !string.IsNullOrEmpty(Error);
        }

        /// <summary>
        /// Determines whether the transaction is complete (either delivered or failed).
        /// </summary>
        /// <returns>True if the transaction is complete, false if still pending.</returns>
        public bool IsComplete()
        {
            return Delivered || (!Pending && HasError());
        }

        /// <summary>
        /// Gets a human-readable status message.
        /// </summary>
        /// <returns>A string describing the current transaction status.</returns>
        public string GetStatusMessage()
        {
            if (!string.IsNullOrEmpty(Error))
            {
                return $"Error: {Error}";
            }
            if (Delivered)
            {
                return "Transaction delivered successfully";
            }
            if (Pending)
            {
                return "Transaction is pending";
            }
            return $"Transaction status: Code {Code}";
        }
    }
}