using System;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for write data transactions.
    /// </summary>
    public class WriteDataBuilder : TransactionBuilder
    {
        private byte[]? _data;
        private string? _format;
        private string? _entryHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteDataBuilder"/> class.
        /// </summary>
        /// <param name="client">The client used to execute transactions.</param>
        public WriteDataBuilder(ApiClient client) : base(client)
        {
        }

        /// <summary>
        /// Sets the data to write.
        /// </summary>
        /// <param name="data">The data as a byte array.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
        public WriteDataBuilder WithData(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            return this;
        }

        /// <summary>
        /// Sets the data to write.
        /// </summary>
        /// <param name="data">The data as a string.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null or empty.</exception>
        public WriteDataBuilder WithData(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            return WithData(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Sets the format of the data.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if format is null or empty.</exception>
        public WriteDataBuilder WithFormat(string format)
        {
            if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
            _format = format;
            return this;
        }

        /// <summary>
        /// Sets the entry hash of the data.
        /// </summary>
        /// <param name="entryHash">The entry hash.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if entryHash is null or empty.</exception>
        public WriteDataBuilder WithEntryHash(string entryHash)
        {
            if (string.IsNullOrEmpty(entryHash)) throw new ArgumentNullException(nameof(entryHash));
            _entryHash = entryHash;
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_data == null)
                throw new InvalidOperationException("Data must be set");
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            Validate();

            var writeData = new WriteData();
            
            // Set required properties
            if (_data != null)
            {
                writeData.WithData(_data);
            }
            
            // Set optional properties if provided
            if (!string.IsNullOrEmpty(_format))
            {
                writeData.WithFormat(_format);
            }
            
            if (!string.IsNullOrEmpty(_entryHash))
            {
                writeData.WithEntryHash(_entryHash);
            }

            return writeData;
        }
    }
} 