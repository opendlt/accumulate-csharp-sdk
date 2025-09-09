using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents an Accumulate Digital Identity (ADI) account.
    /// An ADI is a human-readable identity on the Accumulate network.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ADI : Account
    {
        /// <summary>
        /// Gets the account type, which is IDENTITY.
        /// </summary>
        [JsonProperty("type")]
        public override AccountType Type => AccountType.IDENTITY;

        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public ADI() 
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ADI class.
        /// </summary>
        /// <param name="url">The URL of this ADI.</param>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public ADI(Url url) 
            : base(url)
        {
        }

        /// <summary>
        /// Marshals the ADI to binary format.
        /// </summary>
        /// <returns>The binary representation of the ADI.</returns>
        public override byte[] MarshalBinary()
        {
            // For ADI, we only need the base marshalling
            return base.MarshalBinary();
        }
    }
}