using System;
using System.Text;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a Factom External Reference, holding arbitrary data typically from a string or byte array.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.FactomExtRef.
    /// </summary>
    public class FactomExtRef
    {
        /// <summary>
        /// Gets the data associated with the external reference.
        /// Can be null if constructed from a null string.
        /// </summary>
        public byte[]? Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactomExtRef"/> class with byte array data.
        /// </summary>
        /// <param name="data">The byte array data. Can be null.</param>
        public FactomExtRef(byte[]? data)
        {
            // Store a copy to prevent external modification? Java version didn't copy.
            // Let's match Java for now.
            Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactomExtRef"/> class with string data.
        /// The string is converted to UTF-8 bytes.
        /// </summary>
        /// <param name="data">The string data. Can be null.</param>
        public FactomExtRef(string? data)
        {
            Data = data != null ? Encoding.UTF8.GetBytes(data) : null;
        }
    }
}

