using System;
using Newtonsoft.Json; // For JsonIgnore
using Acme.Net.Sdk.Support; // For Marshaller
using Acme.Net.Sdk.Protocol; // For IMarshallable

namespace Acme.Net.Sdk.Protocol.Generated
{
    /// <summary>
    /// Represents an Accumulate transaction, containing a header and a body.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.generated.protocol.Transaction.
    /// </summary>
    public class Transaction : IMarshallable
    {
        /// <summary>
        /// Gets or sets the transaction header.
        /// </summary>
        public TransactionHeader? Header { get; set; }

        /// <summary>
        /// Gets or sets the transaction body.
        /// </summary>
        public ITransactionBody? Body { get; set; }

        /// <summary>
        /// Gets or sets the pre-computed hash of the transaction (typically set after hashing).
        /// This is ignored during standard JSON serialization.
        /// </summary>
        [JsonIgnore]
        public byte[]? Hash { get; set; }

        /// <summary>
        /// Marshals the transaction header and body into binary format.
        /// Field 1: Header
        /// Field 2: Body
        /// </summary>
        /// <returns>A byte array containing the marshalled data.</returns>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            if (Header != null)
            {
                marshaller.WriteValue(1, Header); // Assumes TransactionHeader implements IMarshallable
            }
            if (Body != null)
            {
                marshaller.WriteValue(2, Body); // Assumes TransactionBody implements IMarshallable
            }
            return marshaller.GetBytes();
        }
    }
}
