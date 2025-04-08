using System;
using System.Text;
using Acme.Net.Sdk.Support;
using Acme.Net.Sdk.Signing;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Provides utility methods for working with Accumulate URLs.
    /// </summary>
    public static class UrlUtils
    {
        /// <summary>
        /// Computes a lite identity URL from a public key.
        /// </summary>
        /// <param name="publicKey">The public key bytes.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        public static Url ComputeLiteIdentityUrl(byte[] publicKey)
        {
            return Principal.ComputeUrl(publicKey);
        }
        
        /// <summary>
        /// Computes a lite identity URL from a SignatureKeyPair.
        /// </summary>
        /// <param name="keyPair">The signature key pair.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keyPair is null.</exception>
        public static Url ComputeLiteIdentityUrl(SignatureKeyPair keyPair)
        {
            if (keyPair == null) throw new ArgumentNullException(nameof(keyPair));
            return ComputeLiteIdentityUrl(keyPair.GetPublicKey());
        }
        
        /// <summary>
        /// Computes a lite token account URL from a public key.
        /// </summary>
        /// <param name="publicKey">The public key bytes.</param>
        /// <param name="tokenUrl">The token URL to use as the authority.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if publicKey or tokenUrl is null.</exception>
        public static Url ComputeLiteTokenAccountUrl(byte[] publicKey, Url tokenUrl)
        {
            if (tokenUrl == null) throw new ArgumentNullException(nameof(tokenUrl));
            return Principal.ComputeUrl(publicKey, tokenUrl);
        }
        
        /// <summary>
        /// Computes a lite token account URL from a SignatureKeyPair.
        /// </summary>
        /// <param name="keyPair">The signature key pair.</param>
        /// <param name="tokenUrl">The token URL to use as the authority.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keyPair or tokenUrl is null.</exception>
        public static Url ComputeLiteTokenAccountUrl(SignatureKeyPair keyPair, Url tokenUrl)
        {
            if (keyPair == null) throw new ArgumentNullException(nameof(keyPair));
            return ComputeLiteTokenAccountUrl(keyPair.GetPublicKey(), tokenUrl);
        }
        
        /// <summary>
        /// Computes a lite token account URL from a public key, using the ACME token as the authority.
        /// </summary>
        /// <param name="publicKey">The public key bytes.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if publicKey is null.</exception>
        public static Url ComputeAcmeLiteTokenAccountUrl(byte[] publicKey)
        {
            var acmeTokenUrl = UrlRegistry.GetInstance().GetAcmeTokenUrl();
            return ComputeLiteTokenAccountUrl(publicKey, acmeTokenUrl);
        }
        
        /// <summary>
        /// Computes a lite token account URL from a SignatureKeyPair, using the ACME token as the authority.
        /// </summary>
        /// <param name="keyPair">The signature key pair.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keyPair is null.</exception>
        public static Url ComputeAcmeLiteTokenAccountUrl(SignatureKeyPair keyPair)
        {
            if (keyPair == null) throw new ArgumentNullException(nameof(keyPair));
            return ComputeAcmeLiteTokenAccountUrl(keyPair.GetPublicKey());
        }
        
        /// <summary>
        /// Computes a lite data account URL (chain ID) from a byte array.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>A lite data account URL.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
        public static Url ComputeLiteDataAccountUrl(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            byte[] hash = HashUtils.Sha256(data);
            string chainId = Convert.ToHexString(hash).ToLowerInvariant();
            
            // Lite data accounts use the plain hash as the authority
            return Url.Parse($"acc://{chainId}");
        }
        
        /// <summary>
        /// Formats an ADI URL with proper path structure.
        /// </summary>
        /// <param name="adiName">The name of the ADI.</param>
        /// <returns>A properly formatted ADI URL.</returns>
        /// <exception cref="ArgumentNullException">Thrown if adiName is null or empty.</exception>
        public static Url FormatAdiUrl(string adiName)
        {
            if (string.IsNullOrEmpty(adiName))
                throw new ArgumentNullException(nameof(adiName));
                
            return Url.Parse($"acc://{adiName}.acme");
        }
        
        /// <summary>
        /// Formats a token account URL within an ADI.
        /// </summary>
        /// <param name="adiUrl">The ADI URL.</param>
        /// <param name="tokenAccountName">The name of the token account.</param>
        /// <returns>A properly formatted token account URL.</returns>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or tokenAccountName is null.</exception>
        public static Url FormatTokenAccountUrl(Url adiUrl, string tokenAccountName)
        {
            if (adiUrl == null) throw new ArgumentNullException(nameof(adiUrl));
            if (string.IsNullOrEmpty(tokenAccountName)) throw new ArgumentNullException(nameof(tokenAccountName));
            
            return Url.Parse($"{adiUrl}/{tokenAccountName}");
        }
        
        /// <summary>
        /// Formats a data account URL within an ADI.
        /// </summary>
        /// <param name="adiUrl">The ADI URL.</param>
        /// <param name="dataAccountName">The name of the data account.</param>
        /// <returns>A properly formatted data account URL.</returns>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or dataAccountName is null.</exception>
        public static Url FormatDataAccountUrl(Url adiUrl, string dataAccountName)
        {
            if (adiUrl == null) throw new ArgumentNullException(nameof(adiUrl));
            if (string.IsNullOrEmpty(dataAccountName)) throw new ArgumentNullException(nameof(dataAccountName));
            
            return Url.Parse($"{adiUrl}/{dataAccountName}");
        }
        
        /// <summary>
        /// Formats a key book URL within an ADI.
        /// </summary>
        /// <param name="adiUrl">The ADI URL.</param>
        /// <param name="keyBookName">The name of the key book.</param>
        /// <returns>A properly formatted key book URL.</returns>
        /// <exception cref="ArgumentNullException">Thrown if adiUrl or keyBookName is null.</exception>
        public static Url FormatKeyBookUrl(Url adiUrl, string keyBookName)
        {
            if (adiUrl == null) throw new ArgumentNullException(nameof(adiUrl));
            if (string.IsNullOrEmpty(keyBookName)) throw new ArgumentNullException(nameof(keyBookName));
            
            return Url.Parse($"{adiUrl}/{keyBookName}");
        }
        
        /// <summary>
        /// Formats a key page URL within a key book.
        /// </summary>
        /// <param name="keyBookUrl">The key book URL.</param>
        /// <param name="keyPageName">The name of the key page.</param>
        /// <returns>A properly formatted key page URL.</returns>
        /// <exception cref="ArgumentNullException">Thrown if keyBookUrl or keyPageName is null.</exception>
        public static Url FormatKeyPageUrl(Url keyBookUrl, string keyPageName)
        {
            if (keyBookUrl == null) throw new ArgumentNullException(nameof(keyBookUrl));
            if (string.IsNullOrEmpty(keyPageName)) throw new ArgumentNullException(nameof(keyPageName));
            
            return Url.Parse($"{keyBookUrl}/{keyPageName}");
        }
        
        /// <summary>
        /// Determines if a URL is a lite identity URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL is a lite identity URL, false otherwise.</returns>
        public static bool IsLiteIdentityUrl(Url url)
        {
            if (url == null) return false;
            
            // A lite identity URL has no path components and the authority is a hash
            return url.Path.Length == 0 && IsHashAuthority(url);
        }
        
        /// <summary>
        /// Determines if a URL is a lite token account URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL is a lite token account URL, false otherwise.</returns>
        public static bool IsLiteTokenAccountUrl(Url url)
        {
            if (url == null) return false;
            
            // A lite token account URL has one path component (the token URL) and the authority is a hash
            string[] pathParts = url.Path.Trim('/').Split('/');
            return pathParts.Length == 1 && IsHashAuthority(url);
        }
        
        /// <summary>
        /// Determines if a URL's authority is a hash (48 hex characters).
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL's authority is a hash, false otherwise.</returns>
        private static bool IsHashAuthority(Url url)
        {
            if (url == null) return false;
            
            // A hash authority is 48 characters (40 + 8) and all hex
            string authority = url.Authority;
            if (authority.Length != 48) return false;
            
            // Check if all characters are hex
            foreach (char c in authority)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            
            return true;
        }
    }
} 