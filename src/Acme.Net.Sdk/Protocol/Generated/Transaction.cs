using System;
using System.Security.Cryptography; 
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
        // src/Acme.Net.Sdk/Protocol/Generated/Transaction.cs
        public byte[] MarshalBinary()
        {
            var m = new Marshaller();

            if (Header != null)
                m.WriteValue(1, Header);   // tag 1 → header TLV

            if (Body != null)
                m.WriteValue(2, Body);     // tag 2 → body TLV

            return m.GetBytes();
        }
        
        /// <summary>
        /// Go-compatible tx hash: SHA256( SHA256(headerTLV) || SHA256(bodyTLV) )
        /// </summary>
        public byte[] GetHash()
        {
            if (Hash != null) return Hash;

            // H(header)
            var headerBytes = Header?.MarshalBinary() ?? Array.Empty<byte>();
            var headerHash  = SHA256.HashData(headerBytes);

            // H(body) or body.GetHash() if the body overrides
            byte[] bodyHash;
            if (Body is IHasCustomHash custom)
            {
                bodyHash = custom.GetHash();
            }
            else
            {
                var bodyBytes = Body?.MarshalBinary() ?? Array.Empty<byte>();
                bodyHash = SHA256.HashData(bodyBytes);
            }

            // SHA256( H(header) || H(body) )
            var concat = new byte[headerHash.Length + bodyHash.Length];
            Buffer.BlockCopy(headerHash, 0, concat, 0, headerHash.Length);
            Buffer.BlockCopy(bodyHash,  0, concat, headerHash.Length, bodyHash.Length);

            Hash = SHA256.HashData(concat);
            return Hash;
        }
    }
}
