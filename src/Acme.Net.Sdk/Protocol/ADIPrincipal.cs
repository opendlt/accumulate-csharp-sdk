using System;
using Acme.Net.Sdk.Protocol.Generated;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a principal associated with an Accumulate Digital Identity (ADI).
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.ADIPrincipal.
    /// </summary>
    public class ADIPrincipal : Principal 
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ADIPrincipal"/> class.
        /// </summary>
        /// <param name="adiUrl">The URL of the ADI account.</param>
        /// <param name="keyPair">The key pair associated with the principal.</param>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or keyPair is null.</exception>
        /// <exception cref="ArgumentException">Thrown if adiUrl is empty or whitespace.</exception>
        public ADIPrincipal(string adiUrl, Acme.Net.Sdk.Signing.SignatureKeyPair keyPair) 
            : base(new ADI(Url.Parse(ValidateAdiUrl(adiUrl))), keyPair ?? throw new ArgumentNullException(nameof(keyPair)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ADIPrincipal"/> class.
        /// </summary>
        /// <param name="adiUrl">The URL of the ADI account.</param>
        /// <param name="keyPair">The key pair associated with the principal.</param>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or keyPair is null.</exception>
        public ADIPrincipal(Url adiUrl, Acme.Net.Sdk.Signing.SignatureKeyPair keyPair)
            : base(new ADI(adiUrl ?? throw new ArgumentNullException(nameof(adiUrl))), 
                   keyPair ?? throw new ArgumentNullException(nameof(keyPair)))
        {
        }

        /// <summary>
        /// Exports the key pair associated with this principal to a base64 string.
        /// </summary>
        /// <returns>Base64 encoded string of the key pair.</returns>
        public string ExportToBase64()
        {
            return base.ExportToBase64(AccountType.IDENTITY);
        }

        /// <summary>
        /// Imports an ADIPrincipal from a base64 encoded key pair string and the ADI URL.
        /// </summary>
        /// <param name="adiUrl">The URL string of the ADI.</param>
        /// <param name="data">The base64 encoded key pair data.</param>
        /// <returns>A new <see cref="ADIPrincipal"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or data is null.</exception>
        /// <exception cref="ArgumentException">Thrown if adiUrl or data is empty or whitespace.</exception>
        public static ADIPrincipal ImportFromBase64(string adiUrl, string data)
        {
            ValidateAdiUrl(adiUrl);
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Base64 data cannot be null or empty", nameof(data));
            }
            return ImportFromBase64(Url.Parse(adiUrl), data);
        }

        /// <summary>
        /// Imports an ADIPrincipal from a base64 encoded key pair string and the ADI URL.
        /// </summary>
        /// <param name="adiUrl">The URL of the ADI.</param>
        /// <param name="data">The base64 encoded key pair data.</param>
        /// <returns>A new <see cref="ADIPrincipal"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or data is null.</exception>
        /// <exception cref="ArgumentException">Thrown if data is empty or whitespace.</exception>
        public static ADIPrincipal ImportFromBase64(Url adiUrl, string data)
        {
            if (adiUrl == null)
            {
                throw new ArgumentNullException(nameof(adiUrl));
            }
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Base64 data cannot be null or empty", nameof(data));
            }
            var keyPair = Principal.ImportKeyPairFromBase64(data);
            return new ADIPrincipal(adiUrl, keyPair);
        }

        private static string ValidateAdiUrl(string adiUrl)
        {
            if (adiUrl == null)
            {
                throw new ArgumentNullException(nameof(adiUrl));
            }
            if (string.IsNullOrWhiteSpace(adiUrl))
            {
                throw new ArgumentException("ADI URL cannot be empty or whitespace", nameof(adiUrl));
            }
            return adiUrl;
        }
    }
}
