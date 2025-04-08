using System;
using System.Text.RegularExpressions;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Provides helper methods for working with Accumulate URLs and patterns.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.UrlRegistry.
    /// </summary>
    public class UrlRegistry
    {
        private static readonly UrlRegistry _instance = new UrlRegistry();
        
        // Constant for the ACME token URL string
        private const string AcmeTokenUrlString = "acc://ACME";
        private static readonly Url _acmeTokenUrl = Url.Parse(AcmeTokenUrlString);

        /// <summary>
        /// Gets the singleton instance of the UrlRegistry.
        /// </summary>
        /// <returns>The singleton UrlRegistry instance.</returns>
        public static UrlRegistry GetInstance()
        {
            return _instance;
        }
        
        /// <summary>
        /// Gets the predefined URL for the ACME token.
        /// </summary>
        /// <returns>The <see cref="Url"/> object representing acc://ACME.</returns>
        public Url GetAcmeTokenUrl()
        {
            return _acmeTokenUrl;
        }

        // ... GetAcmeTokenUrl ...
        // ... IsMatch ...

        /// <summary>
        /// Checks if a given URL string represents a valid Lite Address format.
        /// </summary>
        /// <param name="url">The URL string to check.</param>
        /// <returns>True if the URL is a valid Lite Address, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public bool IsLiteAddress(string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            try
            {
                // Attempt to construct a LiteAddress. If it succeeds and IsValid is true, it's a valid format.
                var liteAddress = new LiteAddress(url);
                return liteAddress.IsValid;
            }
            catch (UriFormatException) // Catch errors from base Url parsing
            {
                return false;
            }
            catch (ArgumentNullException) // Should be caught by initial check, but for safety
            {
                 return false;
            }
            // Add catch for other potential exceptions if necessary
        }

        /// <summary>
        /// Checks if a given URL string represents a valid Lite Token Address format.
        /// </summary>
        /// <param name="url">The URL string to check.</param>
        /// <returns>True if the URL is a valid Lite Token Address, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if url is null.</exception>
        public bool IsLiteTokenAddress(string url)
        {
             if (url == null) throw new ArgumentNullException(nameof(url));

            try
            {
                // Attempt to construct a LiteTokenAddress. If it succeeds and IsValid is true, it's valid.
                var liteTokenAddress = new LiteTokenAddress(url);
                return liteTokenAddress.IsValid;
            }
            catch (UriFormatException) // Catch errors from base Url parsing
            {
                return false;
            }
             catch (ArgumentNullException) // Should be caught by initial check, but for safety
            {
                 return false;
            }
             // Add catch for other potential exceptions if necessary
        }
    }
}
