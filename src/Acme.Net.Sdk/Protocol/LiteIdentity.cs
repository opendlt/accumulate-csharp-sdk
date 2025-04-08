using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a Lite Identity Account in the Accumulate protocol.
    /// A lite identity is an identity that is not part of an ADI.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class LiteIdentity : Account
    {
        /// <summary>
        /// Gets the account type, which is LITE_IDENTITY.
        /// </summary>
        [JsonProperty("type")]
        public override AccountType Type => AccountType.LITE_IDENTITY;

        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public LiteIdentity() 
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the LiteIdentity class.
        /// </summary>
        /// <param name="url">The URL of this account.</param>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public LiteIdentity(Url url) 
            : base(url)
        {
        }

        /// <summary>
        /// Marshals the account to binary format.
        /// </summary>
        /// <returns>The binary representation of the account.</returns>
        public override byte[] MarshalBinary()
        {
            // For LiteIdentity, we only need the base marshalling
            return base.MarshalBinary();
        }
    }
} 