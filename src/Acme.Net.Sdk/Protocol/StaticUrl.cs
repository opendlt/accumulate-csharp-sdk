using System;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents static URL constants for the Acme network.
    /// </summary>
    public enum StaticUrl
    {
        /// <summary>
        /// The ACME token URL.
        /// </summary>
        ACME_TOKEN_URL
    }

    /// <summary>
    /// Extension methods for the StaticUrl enum.
    /// </summary>
    public static class StaticUrlExtensions
    {
        /// <summary>
        /// Gets the Url value for the specified StaticUrl.
        /// </summary>
        /// <param name="staticUrl">The StaticUrl enum value.</param>
        /// <returns>The corresponding Url.</returns>
        public static Url GetValue(this StaticUrl staticUrl)
        {
            return staticUrl switch
            {
                StaticUrl.ACME_TOKEN_URL => Url.Parse("acc://ACME"),
                _ => throw new ArgumentOutOfRangeException(nameof(staticUrl), staticUrl, "Unknown StaticUrl value")
            };
        }

        /// <summary>
        /// Matches a URL string to a StaticUrl enum value based on prefix.
        /// </summary>
        /// <param name="url">The URL string to match.</param>
        /// <returns>The matching StaticUrl enum value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the url is null.</exception>
        /// <exception cref="ArgumentException">Thrown when no matching StaticUrl is found.</exception>
        public static StaticUrl MatchPrefix(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url), "URL value may not be empty");
            }

            url = url.ToLowerInvariant();

            foreach (StaticUrl item in Enum.GetValues(typeof(StaticUrl)))
            {
                string itemUrl = item.GetValue().String().ToLowerInvariant();
                if (itemUrl.StartsWith(url))
                {
                    return item;
                }
            }

            throw new ArgumentException($"Can't match a StaticUrl for {url}", nameof(url));
        }
    }
}
