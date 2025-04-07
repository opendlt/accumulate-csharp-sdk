using System;
using System.Threading;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// Builder for creating and executing transactions that write data to a specific account.
    /// </summary>
    public class WriteDataToBuilder : TransactionBuilder
    {
        private Url? _recipient;
        private byte[]? _data;
        private string? _format;
        private string? _entryHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteDataToBuilder"/> class.
        /// </summary>
        /// <param name="apiClient">The API client.</param>
        public WriteDataToBuilder(ApiClient apiClient)
            : base(apiClient)
        {
        }

        /// <summary>
        /// Sets the recipient account URL.
        /// </summary>
        /// <param name="recipient">The recipient URL.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null.</exception>
        public WriteDataToBuilder WithRecipient(Url recipient)
        {
            _recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            return this;
        }

        /// <summary>
        /// Sets the recipient account URL.
        /// </summary>
        /// <param name="recipient">The recipient URL as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if recipient is null or empty.</exception>
        public WriteDataToBuilder WithRecipient(string recipient)
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
        public WriteDataToBuilder WithData(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            return this;
        }

        /// <summary>
        /// Sets the data to write.
        /// </summary>
        /// <param name="data">The data as a string.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null or empty.</exception>
        public WriteDataToBuilder WithData(string data)
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
        public WriteDataToBuilder WithFormat(string format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
            return this;
        }

        /// <summary>
        /// Sets the entry hash of the data.
        /// </summary>
        /// <param name="entryHash">The entry hash.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if entryHash is null or empty.</exception>
        public WriteDataToBuilder WithEntryHash(string entryHash)
        {
            _entryHash = entryHash ?? throw new ArgumentNullException(nameof(entryHash));
            return this;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (_recipient == null)
            {
                throw new InvalidOperationException("Recipient URL must be set.");
            }

            if (_data == null)
            {
                throw new InvalidOperationException("Data must be set.");
            }
        }

        /// <inheritdoc/>
        protected override ITransactionBody BuildTransactionBody()
        {
            var body = new WriteDataTo()
                .WithRecipient(_recipient!);

            if (_data != null)
            {
                body.WithData(_data);
            }

            if (!string.IsNullOrEmpty(_format))
            {
                body.WithFormat(_format);
            }

            if (!string.IsNullOrEmpty(_entryHash))
            {
                body.WithEntryHash(_entryHash);
            }

            return body;
        }
    }
} 