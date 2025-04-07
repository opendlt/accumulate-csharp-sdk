using System;
using System.Collections.Generic;
using System.Text;
using Acme.Net.Sdk.Support; // For HashBuilder

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a Factom data entry, including main data and external references (ExtRefs).
    /// Used for calculating Factom Chain IDs.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.FactomEntry.
    /// </summary>
    public class FactomEntry
    {
        /// <summary>
        /// Gets the main data content of the entry.
        /// Can be null.
        /// </summary>
        public byte[]? Data { get; }

        /// <summary>
        /// Gets the list of external references associated with the entry.
        /// </summary>
        public List<FactomExtRef> ExtRefs { get; } = new List<FactomExtRef>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FactomEntry"/> class with null data.
        /// </summary>
        public FactomEntry() 
        {
            Data = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactomEntry"/> class with byte array data.
        /// </summary>
        /// <param name="data">The main byte array data. Can be null.</param>
        public FactomEntry(byte[]? data)
        {
            Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactomEntry"/> class with string data.
        /// The string is converted to UTF-8 bytes.
        /// </summary>
        /// <param name="data">The main string data. Can be null.</param>
        public FactomEntry(string? data)
        {
            Data = data != null ? Encoding.UTF8.GetBytes(data) : null;
        }

        /// <summary>
        /// Adds an external reference from byte array data.
        /// </summary>
        /// <param name="data">The ExtRef data. Can be null.</param>
        /// <returns>The current <see cref="FactomEntry"/> instance for chaining.</returns>
        public FactomEntry AddExtRef(byte[]? data)
        {
            ExtRefs.Add(new FactomExtRef(data));
            return this;
        }

        /// <summary>
        /// Adds an external reference from string data (converted to UTF-8 bytes).
        /// </summary>
        /// <param name="data">The ExtRef data. Can be null.</param>
        /// <returns>The current <see cref="FactomEntry"/> instance for chaining.</returns>
        public FactomEntry AddExtRef(string? data)
        {
            ExtRefs.Add(new FactomExtRef(data));
            return this;
        }

        /// <summary>
        /// Adds an existing external reference object.
        /// </summary>
        /// <param name="extRef">The <see cref="FactomExtRef"/> object to add.</param>
        /// <returns>The current <see cref="FactomEntry"/> instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if extRef is null.</exception>
        public FactomEntry AddExtRef(FactomExtRef extRef)
        {
            if (extRef == null) throw new ArgumentNullException(nameof(extRef));
            ExtRefs.Add(extRef);
            return this;
        }

        /// <summary>
        /// Calculates the Factom Chain ID based on the SHA256 checksum of the external references.
        /// </summary>
        /// <returns>A 32-byte array representing the Chain ID.</returns>
        public byte[] CalculateChainId()
        {
            var hashBuilder = new HashBuilder();
            foreach (var extRef in ExtRefs)
            {
                // AddBytes handles null input gracefully (ignores it)
                hashBuilder.AddBytes(extRef.Data);
            }
            // The checksum calculation in HashBuilder already returns the 32-byte SHA256 hash
            return hashBuilder.GetCheckSum();
        }

        /// <summary>
        /// Creates an Accumulate URL representation of the Factom Chain ID.
        /// The format is acc://{hex(chain_id)}.
        /// </summary>
        /// <returns>An Accumulate <see cref="Url"/>.</returns>
        public Url ToUrl()
        {
            byte[] chainId = CalculateChainId();
            string hexChainId = Convert.ToHexString(chainId).ToLowerInvariant(); // Use lowercase hex for consistency
            return Url.Parse($"acc://{hexChainId}");
        }
    }
}

