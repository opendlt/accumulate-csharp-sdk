using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents a transaction body for updating a key page.
    /// Adds, removes, or updates keys in a key page.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [JsonConverter(typeof(TransactionBodyConverter))]
    public class UpdateKeyPage : ITransactionBody
    {
        /// <summary>
        /// Gets the transaction type.
        /// </summary>
        [JsonProperty("type")]
        public string Type => "updateKeyPage";

        /// <summary>
        /// Gets or sets the key page operations.
        /// </summary>
        [JsonProperty("operations")]
        public List<IKeyPageOperation> Operations { get; set; } = new List<IKeyPageOperation>();

        /// <summary>
        /// Creates a new instance of UpdateKeyPage.
        /// </summary>
        public UpdateKeyPage()
        {
        }

        /// <summary>
        /// Adds an operation to the key page update.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The current instance for method chaining.</returns>
        public UpdateKeyPage AddOperation(IKeyPageOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            Operations.Add(operation);
            return this;
        }

        /// <inheritdoc/>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            
            // Marshal type as field 1
            marshaller.WriteUInt(1, TransactionTypeCode.UpdateKeyPage);
            
            // Marshal operations as field 2 (repeatable)
            // In JavaScript SDK, this is repeatable KeyPageOperation union array
            if (Operations != null && Operations.Count > 0)
            {
                foreach (var operation in Operations)
                {
                    // TODO: Implement proper KeyPageOperation marshalling
                    // For now, we'll need to implement the operation types
                    byte[] operationBytes = operation.MarshalBinary();
                    marshaller.WriteBytes(2, operationBytes);
                }
            }
            
            return marshaller.GetBytes();
        }
    }
}