using System;
using System.Numerics;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Support.Serializers;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a Lite Token Account in the Accumulate protocol.
    /// A lite token account is a token account that is not associated with an ADI.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class LiteTokenAccount : Account
    {
        private Url _tokenUrl;
        private BigInteger _balance;
        private long _lockHeight;

        /// <summary>
        /// Gets the account type, which is LITE_TOKEN_ACCOUNT.
        /// </summary>
        [JsonProperty("type")]
        public override AccountType Type => AccountType.LITE_TOKEN_ACCOUNT;

        /// <summary>
        /// Gets or sets the token URL for this account.
        /// </summary>
        [JsonProperty("tokenUrl")]
        public Url TokenUrl 
        { 
            get => _tokenUrl; 
            set => _tokenUrl = value ?? throw new ArgumentNullException(nameof(value)); 
        }

        /// <summary>
        /// Gets or sets the balance of the account.
        /// </summary>
        [JsonProperty("balance")]
        public BigInteger Balance
        {
            get => _balance;
            set => _balance = value;
        }

        /// <summary>
        /// Gets or sets the lock height of the account.
        /// </summary>
        [JsonProperty("lockHeight")]
        public long LockHeight
        {
            get => _lockHeight;
            set => _lockHeight = value;
        }

        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public LiteTokenAccount() 
            : base()
        {
            _tokenUrl = null!;
            _balance = BigInteger.Zero;
            _lockHeight = 0;
        }

        /// <summary>
        /// Initializes a new instance of the LiteTokenAccount class.
        /// </summary>
        /// <param name="url">The URL of this account.</param>
        /// <param name="tokenUrl">The URL of the token.</param>
        /// <exception cref="ArgumentNullException">Thrown if url or tokenUrl is null.</exception>
        public LiteTokenAccount(Url url, Url tokenUrl) 
            : base(url)
        {
            _tokenUrl = tokenUrl ?? throw new ArgumentNullException(nameof(tokenUrl));
            _balance = BigInteger.Zero;
            _lockHeight = 0;
        }

        /// <summary>
        /// Fluent setter for the token URL.
        /// </summary>
        /// <param name="value">The token URL to set.</param>
        /// <returns>This instance for method chaining.</returns>
        public LiteTokenAccount WithTokenUrl(Url value)
        {
            TokenUrl = value;
            return this;
        }

        /// <summary>
        /// Fluent setter for the token URL from a string.
        /// </summary>
        /// <param name="value">The token URL string to set.</param>
        /// <returns>This instance for method chaining.</returns>
        public LiteTokenAccount WithTokenUrl(string value)
        {
            TokenUrl = Url.Parse(value);
            return this;
        }

        /// <summary>
        /// Fluent setter for the balance.
        /// </summary>
        /// <param name="value">The balance to set.</param>
        /// <returns>This instance for method chaining.</returns>
        public LiteTokenAccount WithBalance(BigInteger value)
        {
            Balance = value;
            return this;
        }

        /// <summary>
        /// Fluent setter for the lock height.
        /// </summary>
        /// <param name="value">The lock height to set.</param>
        /// <returns>This instance for method chaining.</returns>
        public LiteTokenAccount WithLockHeight(long value)
        {
            LockHeight = value;
            return this;
        }

        /// <summary>
        /// Marshals the account to binary format.
        /// </summary>
        /// <returns>The binary representation of the account.</returns>
        public override byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            marshaller.WriteValue(1, Type);
            
            if (Url != null)
            {
                marshaller.WriteUrl(2, Url);
            }
            
            if (_tokenUrl != null)
            {
                marshaller.WriteUrl(3, _tokenUrl);
            }
            
            if (_balance != BigInteger.Zero)
            {
                marshaller.WriteBigInt(4, _balance);
            }
            
            if (_lockHeight != 0)
            {
                marshaller.WriteUInt(5, _lockHeight);
            }
            
            return marshaller.ToArray();
        }
    }
} 