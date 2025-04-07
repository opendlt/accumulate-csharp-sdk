using System.Text.RegularExpressions;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Defines URL patterns for different types of Acme network URLs.
    /// </summary>
    public enum UrlPattern
    {
        /// <summary>
        /// Pattern for a lite address.
        /// </summary>
        LITE_ADDRESS,

        /// <summary>
        /// Pattern for a lite token address.
        /// </summary>
        LITE_TOKEN_ADDRESS,

        /// <summary>
        /// Pattern for a directory network URL.
        /// </summary>
        DN_URL,

        /// <summary>
        /// Pattern for a basic validation network URL.
        /// </summary>
        BVN_URL
    }

    /// <summary>
    /// Extension methods for the UrlPattern enum.
    /// </summary>
    public static class UrlPatternExtensions
    {
        /// <summary>
        /// Gets the regular expression pattern for the specified URL pattern.
        /// </summary>
        /// <param name="urlPattern">The URL pattern.</param>
        /// <returns>The regular expression pattern, or null if not applicable.</returns>
        public static Regex GetPattern(this UrlPattern urlPattern)
        {
            return urlPattern switch
            {
                UrlPattern.LITE_ADDRESS => null,
                UrlPattern.LITE_TOKEN_ADDRESS => null,
                UrlPattern.DN_URL => new Regex(@"^(acc://dnn)"),
                UrlPattern.BVN_URL => new Regex(@"^(acc://bvnn)"),
                _ => null
            };
        }
    }
}
