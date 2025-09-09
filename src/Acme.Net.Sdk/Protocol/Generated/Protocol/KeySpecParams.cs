using System;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents the parameters for a key specification.
    /// Corresponds to JavaScript SDK KeySpecParams class.
    /// </summary>
    public class KeySpecParams : IMarshallable
    {
        /// <summary>
        /// Gets or sets the key hash (field 1).
        /// </summary>
        public byte[]? KeyHash { get; set; }

        /// <summary>
        /// Gets or sets the delegate URL (field 2).
        /// </summary>
        public Url? Delegate { get; set; }

        /// <summary>
        /// Initializes a new instance of the KeySpecParams class.
        /// </summary>
        public KeySpecParams()
        {
        }

        /// <summary>
        /// Initializes a new instance of the KeySpecParams class with a key hash.
        /// </summary>
        /// <param name="keyHash">The key hash.</param>
        public KeySpecParams(byte[] keyHash)
        {
            KeyHash = keyHash;
        }

        /// <summary>
        /// Initializes a new instance of the KeySpecParams class with a key hash and delegate.
        /// </summary>
        /// <param name="keyHash">The key hash.</param>
        /// <param name="delegate">The delegate URL.</param>
        public KeySpecParams(byte[] keyHash, Url? @delegate)
        {
            KeyHash = keyHash;
            Delegate = @delegate;
        }

        /// <summary>
        /// Marshals the KeySpecParams to binary format.
        /// </summary>
        /// <returns>The binary representation.</returns>
        public byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();

            // Field 1: keyHash (bytes)
            if (KeyHash != null && KeyHash.Length > 0)
            {
                marshaller.WriteBytes(1, KeyHash);
            }

            // Field 2: delegate (URL)
            if (Delegate != null)
            {
                marshaller.WriteUrl(2, Delegate);
            }

            return marshaller.GetBytes();
        }
    }
}