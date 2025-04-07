using System;
using System.Text;
using Newtonsoft.Json;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents an Accumulate URL (acc://...).
    /// Provides parsing and helper methods specific to Accumulate URLs.
    /// Uses a custom JsonConverter for string-based serialization.
    /// </summary>
    [JsonConverter(typeof(UrlConverter))]
    public class Url
    {
        private readonly Uri _uri;

        /// <summary>
        /// Gets the underlying System.Uri object.
        /// </summary>
        public Uri Uri => _uri;

        // Internal constructor for JsonConverter
        internal Url(Uri uri)
        {
            // Validation is done in Parse/Constructors
            _uri = uri;
        }

        /// <summary>
        /// Constructs an Accumulate Url from a string.
        /// Ensures the string is a valid URI and has the 'acc' scheme (or adds it).
        /// </summary>
        /// <param name="value">The URL string.</param>
        /// <exception cref="ArgumentNullException">Thrown if value is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the value is not a valid URI or is missing authority.</exception>
        public Url(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            
            _uri = ParseInternal(value);
            
            // Java version checked host emptiness after creating URI, let's do similar check
            if (string.IsNullOrEmpty(_uri.Host))
            {
                 throw new UriFormatException("Accumulate URL must have an authority (host).");
            }
        }

        /// <summary>
        /// Parses a string into an Accumulate Url object.
        /// Adds "acc://" scheme if missing.
        /// </summary>
        /// <param name="str">The URL string to parse.</param>
        /// <returns>An Url object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if str is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the resulting string is not a valid URI or is missing authority.</exception>
        public static Url Parse(string str)
        {
             if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException(nameof(str));
            }
            Uri parsedUri = ParseInternal(str);
            if (string.IsNullOrEmpty(parsedUri.Host))
            {
                 throw new UriFormatException("Accumulate URL must have an authority (host).");
            }
            return new Url(parsedUri);
        }

        // Internal parsing logic to handle scheme addition
        private static Uri ParseInternal(string str)
        {
             string uriString = str;
            if (!uriString.Contains("://"))
            {
                uriString = "acc://" + uriString;
            }
            else if (!uriString.StartsWith("acc://", StringComparison.OrdinalIgnoreCase))
            {   
                // Consider if non-'acc' schemes should be allowed or rejected
                // For strict porting, let Uri constructor handle it, but could add check.
                // throw new UriFormatException("Scheme must be 'acc'");
            }

            try
            {
                 // Use Uri constructor which performs validation
                return new Uri(uriString, UriKind.Absolute);
            }
            catch (UriFormatException e)
            {
                // Wrap exception if needed, or rethrow
                throw new UriFormatException($"Invalid Accumulate URL format: {str}", e);
            }
        }

        /// <summary>
        /// Gets the authority part of the URL (host and port).
        /// </summary>
        public string Authority => _uri.Authority;

        /// <summary>
        /// Gets the host name part of the URL.
        /// </summary>
        public string HostName => _uri.Host;

        /// <summary>
        /// Gets the absolute path of the URL, stripping any trailing slash.
        /// </summary>
        public string Path
        {
            get 
            {
                 // Uri.AbsolutePath includes leading slash, Java's getPath did not necessarily
                // StringUtils.stripEnd matches TrimEnd
                return _uri.AbsolutePath.TrimEnd('/');
            }
        }

        /// <summary>
        /// Gets the query part of the URL (including the leading ?).
        /// </summary>
        public string Query => _uri.Query;

        // Note: Java fragment() returned Authority, which seems wrong. 
        // C# Fragment corresponds to #fragment part.
        /// <summary>
        /// Gets the fragment part of the URL (including the leading #).
        /// </summary>
        public string Fragment => _uri.Fragment;

        /// <summary>
        /// Gets the full string representation of the URL.
        /// Trims trailing slash if the path is empty.
        /// </summary>
        /// <returns>The URL string.</returns>
        public string String() { 
            string s = _uri.ToString();
            // If the original Uri had an empty path, ToString() adds a trailing /. Remove it.
            if ((_uri.AbsolutePath == "" || _uri.AbsolutePath == "/") && s.EndsWith("/")) 
            {
                return s.TrimEnd('/');
            }
            return s;
        }

        /// <summary>
        /// Returns a new Url representing the root (scheme and authority) of the current URL.
        /// </summary>
        /// <returns>The root Url.</returns>
        public Url GetRootUrl()
        {
            // Construct string manually to ensure acc:// scheme
            string root = $"acc://{Authority}";
            return Url.Parse(root);
        }

        /// <summary>
        /// Returns a new Url representing the parent path of the current URL.
        /// </summary>
        /// <returns>The parent Url.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current URL is already the root.</exception>
        public Url GetParentUrl()
        {
            string currentPath = Path; // Gets path without trailing slash
            // If path is empty or just "/", it's the root or has no parent path
            if (string.IsNullOrEmpty(currentPath) || currentPath == "/")
            {
                throw new InvalidOperationException("URL is already the root or has no parent path.");
            }

            int cutPos = currentPath.LastIndexOf('/');
            
            // If no slash found, or only at the beginning, parent is the root
            if (cutPos <= 0) 
            {
                return GetRootUrl();
            }
            else
            {
                string parentPath = currentPath.Substring(0, cutPos);
                // Construct string manually
                 string parent = $"acc://{Authority}{parentPath}";
                return Url.Parse(parent);
            }
        }

        public override bool Equals(object? obj)
        {
            // Use Uri comparison
            return obj is Url url && object.Equals(_uri, url._uri);
        }

        public override int GetHashCode()
        {
            // Use Uri hash code
            return _uri?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return String();
        }
    }

    /// <summary>
    /// Custom JsonConverter for Url serialization/deserialization as a string.
    /// </summary>
    public class UrlConverter : JsonConverter<Url>
    {
        public override void WriteJson(JsonWriter writer, Url? value, JsonSerializer serializer)
        {
            // Write the string representation
            writer.WriteValue(value?.String());
        }

        public override Url? ReadJson(JsonReader reader, Type objectType, Url? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonToken.String)
            {
                string? urlString = reader.Value as string;
                if (urlString != null)
                {
                    try
                    {
                        // Use the Url.Parse method for validation and construction
                        return Url.Parse(urlString);
                    }
                    catch (Exception e) when (e is UriFormatException || e is ArgumentNullException)
                    {
                        // Wrap parsing errors in JsonSerializationException
                        throw new JsonSerializationException($"Error parsing Accumulate URL string: {urlString}", e);
                    }
                }
            }
            throw new JsonSerializationException($"Unexpected token type for Url: {reader.TokenType}");
        }
    }
} 