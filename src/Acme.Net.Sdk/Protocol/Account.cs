using System;
using Newtonsoft.Json;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Abstract base class for all Accumulate account types.
    /// Provides a common implementation for the IAccount interface.
    /// </summary>
    public abstract class Account : IAccount
    {
        private Url _url;

        /// <summary>
        /// Gets the type of the account.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract AccountType Type { get; }

        /// <summary>
        /// Gets or sets the URL of the account.
        /// </summary>
        public Url Url 
        { 
            get => _url;
            set => _url = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        protected Account()
        {
            _url = null!; // Will be set by deserialization
        }

        /// <summary>
        /// Initializes a new instance of the Account class with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the account.</param>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        protected Account(Url url)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
        }

        /// <summary>
        /// Marshals the account to binary format.
        /// Base implementation for all account types.
        /// </summary>
        /// <returns>The binary representation of the account.</returns>
        public virtual byte[] MarshalBinary()
        {
            var marshaller = new Marshaller();
            marshaller.WriteValue(1, Type);
            
            if (_url != null)
            {
                marshaller.WriteUrl(2, _url);
            }
            
            return marshaller.ToArray();
        }

        /// <summary>
        /// Returns a string representation of the account, which is its URL.
        /// </summary>
        /// <returns>The URL as a string.</returns>
        public override string ToString()
        {
            return _url?.ToString() ?? "Account (no URL)";
        }
    }
} 