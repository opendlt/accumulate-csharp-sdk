using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for writing data to a specific data account.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class WriteDataTo : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "writeDataTo";

        /// <summary>
        /// Gets or sets the recipient URL.
        /// </summary>
        [JsonProperty("recipient")]
        public Url? Recipient { get; set; }

        /// <summary>
        /// Gets or sets the data to write.
        /// </summary>
        [JsonProperty("data")]
        public byte[]? Data { get; set; }

        /// <summary>
        /// Gets or sets the format of the data.
        /// </summary>
        [JsonProperty("format")]
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the entry hash of the data.
        /// </summary>
        [JsonProperty("entryHash")]
        public string? EntryHash { get; set; }

        /// <summary>
        /// Sets the recipient URL.
        /// </summary>
        /// <param name="recipient">The recipient URL.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null.</exception>
        public WriteDataTo WithRecipient(Url recipient)
        {
            Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            return this;
        }

        /// <summary>
        /// Sets the recipient URL.
        /// </summary>
        /// <param name="recipient">The recipient URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null or empty.</exception>
        public WriteDataTo WithRecipient(string recipient)
        {
            if (string.IsNullOrEmpty(recipient)) throw new ArgumentNullException(nameof(recipient));
            return WithRecipient(new Url(recipient));
        }

        /// <summary>
        /// Sets the data to write.
        /// </summary>
        /// <param name="data">The data as a byte array.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
        public WriteDataTo WithData(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            return this;
        }

        /// <summary>
        /// Sets the data to write.
        /// </summary>
        /// <param name="data">The data as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null or empty.</exception>
        public WriteDataTo WithData(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            return WithData(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Sets the format of the data.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if format is null or empty.</exception>
        public WriteDataTo WithFormat(string format)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            return this;
        }

        /// <summary>
        /// Sets the entry hash of the data.
        /// </summary>
        /// <param name="entryHash">The entry hash.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if entryHash is null or empty.</exception>
        public WriteDataTo WithEntryHash(string entryHash)
        {
            EntryHash = entryHash ?? throw new ArgumentNullException(nameof(entryHash));
            return this;
        }

        /// <summary>
        /// Field for storing the DataEntry when needed for test vectors.
        /// </summary>
        public IDataEntry? DataEntry { get; set; }
        
        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Field 1: type (CRITICAL - was missing!)
            marshaller.WriteUInt(1, TransactionTypeCode.WriteDataTo);
            
            // Field 2: recipient
            if (Recipient != null)
            {
                marshaller.WriteValue(2, Recipient);
            }
            
            // Field 3: entry (DataEntry union)
            if (DataEntry != null)
            {
                // Use the explicit DataEntry if set (for test vectors)
                marshaller.WriteValue(3, DataEntry);
            }
            else if (Data != null || !string.IsNullOrEmpty(Format) || !string.IsNullOrEmpty(EntryHash))
            {
                // For backward compatibility, create an AccumulateDataEntry from the legacy fields
                var entry = DataEntryHelper.FromLegacyFields(Data, Format, EntryHash);
                marshaller.WriteValue(3, entry);
            }
            
            return marshaller.GetBytes();
        }
    }
} 