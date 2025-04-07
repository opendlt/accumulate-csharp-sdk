using System.Text;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Extension methods for the Url class.
    /// </summary>
    public static class UrlExtensions
    {
        /// <summary>
        /// Gets the UTF-8 bytes representation of the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The UTF-8 encoded bytes of the URL string.</returns>
        public static byte[] GetBytes(this Url url)
        {
            return Encoding.UTF8.GetBytes(url.String());
        }
    }
} 