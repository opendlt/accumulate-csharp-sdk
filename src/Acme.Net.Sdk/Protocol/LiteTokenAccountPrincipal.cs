using System;
using NSec.Cryptography; // For Key, SignatureAlgorithm, KeyCreationParameters, KeyExportPolicies
using Acme.Net.Sdk.Protocol.Generated; // Reverted to correct namespace for AccountType, SignatureType
using Acme.Net.Sdk.Protocol; // Keep for Url, IAccount etc.
using Acme.Net.Sdk.Signing; // For the centralized SignatureKeyPair

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a principal associated with an Accumulate Lite Token Account.
    /// A Lite Token Account URL is derived from the public key and the Token Issuer URL.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.protocol.LiteTokenAccountPrincipal.
    /// </summary>
    public class LiteTokenAccountPrincipal : Principal
    {
        private static readonly UrlRegistry _urlRegistry = new UrlRegistry();

        /// <summary>
        /// Gets the Lite Token Account associated with this principal.
        /// </summary>
        public LiteTokenAccount LiteTokenAccount => Account as LiteTokenAccount ?? 
            throw new InvalidOperationException("Account is not a LiteTokenAccount");

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteTokenAccountPrincipal"/> class for the ACME token.
        /// Uses the default ACME token URL and computes the account URL based on the key pair.
        /// </summary>
        /// <param name="keyPair">The key pair for this principal.</param>
        /// <exception cref="ArgumentNullException">Thrown if keyPair is null.</exception>
        public LiteTokenAccountPrincipal(Acme.Net.Sdk.Signing.SignatureKeyPair keyPair)
            : this(_urlRegistry.GetAcmeTokenUrl(), keyPair) // Delegate to the other constructor
        {
            // Null check for keyPair happens in the delegated constructor or base class
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteTokenAccountPrincipal"/> class for a specific token URL.
        /// Computes the account URL based on the key pair and the provided token URL.
        /// </summary>
        /// <param name="tokenUrl">The URL of the token issuer for this account.</param>
        /// <param name="keyPair">The key pair for this principal.</param>
        /// <exception cref="ArgumentNullException">Thrown if tokenUrl or keyPair is null.</exception>
        public LiteTokenAccountPrincipal(Url tokenUrl, Acme.Net.Sdk.Signing.SignatureKeyPair keyPair)
            : base(CreateLiteTokenAccount(keyPair, tokenUrl), keyPair)
        {
            // Base constructor handles assignment
        }

        // Helper method to create a LiteTokenAccount with computed URL
        private static LiteTokenAccount CreateLiteTokenAccount(Acme.Net.Sdk.Signing.SignatureKeyPair keyPair, Url tokenUrl)
        {
            if (keyPair == null) throw new ArgumentNullException(nameof(keyPair));
            if (tokenUrl == null) throw new ArgumentNullException(nameof(tokenUrl));

            var accountUrl = ComputeUrl(keyPair.GetPublicKey(), tokenUrl);
            return new LiteTokenAccount(accountUrl, tokenUrl);
        }

        /// <summary>
        /// Generates a new random Lite Token Account Principal for the ACME token with the specified signature type.
        /// </summary>
        /// <param name="signatureType">The type of signature algorithm to use (e.g., ED25519).</param>
        /// <returns>A new <see cref="LiteTokenAccountPrincipal"/> with a generated key pair.</returns>
        /// <exception cref="NotSupportedException">Thrown if the specified signature type is not supported for key generation.</exception>
        public static LiteTokenAccountPrincipal Generate(SignatureType signatureType)
        {
            // Generate key pair
            var keyPair = GenerateNewKeyPair(signatureType);
            // Create principal using the constructor that defaults to ACME token url
            return new LiteTokenAccountPrincipal(keyPair);
        }

        /// <summary>
        /// Generates a new random Lite Token Account Principal for the specified token URL and signature type.
        /// </summary>
        /// <param name="tokenUrl">The URL of the token issuer.</param>
        /// <param name="signatureType">The type of signature algorithm to use.</param>
        /// <returns>A new <see cref="LiteTokenAccountPrincipal"/>.</returns>
         /// <exception cref="ArgumentNullException">Thrown if tokenUrl is null.</exception>
        /// <exception cref="NotSupportedException">Thrown if the specified signature type is not supported for key generation.</exception>
        public static LiteTokenAccountPrincipal GenerateWithTokenUrl(Url tokenUrl, SignatureType signatureType)
        {
            if (tokenUrl == null) throw new ArgumentNullException(nameof(tokenUrl));
            var keyPair = GenerateNewKeyPair(signatureType);
            return new LiteTokenAccountPrincipal(tokenUrl, keyPair);
        }

        /// <summary>
        /// Generates a new random Lite Token Account Principal for the specified token URL string and signature type.
        /// </summary>
        /// <param name="tokenUrlString">The URL string of the token issuer.</param>
        /// <param name="signatureType">The type of signature algorithm to use.</param>
        /// <returns>A new <see cref="LiteTokenAccountPrincipal"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if tokenUrlString is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if tokenUrlString is not a valid URL format.</exception>
        /// <exception cref="NotSupportedException">Thrown if the specified signature type is not supported for key generation.</exception>
        public static LiteTokenAccountPrincipal GenerateWithTokenUrl(string tokenUrlString, SignatureType signatureType)
        {
             if (string.IsNullOrEmpty(tokenUrlString)) throw new ArgumentException("Token URL string cannot be null or empty.", nameof(tokenUrlString));
             return GenerateWithTokenUrl(Url.Parse(tokenUrlString), signatureType);
        }

        // Helper to generate a new key pair
        private static Acme.Net.Sdk.Signing.SignatureKeyPair GenerateNewKeyPair(SignatureType signatureType)
        {
            SignatureAlgorithm algorithm =
                signatureType == SignatureType.ED25519
                    ? SignatureAlgorithm.Ed25519
                    : throw new NotSupportedException($"Generating key pairs for signature type {signatureType} is not currently supported.");

            var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
            using var key = Key.Create(algorithm, creationParams);

            var seed32 = key.Export(KeyBlobFormat.RawPrivateKey);

            if (!Acme.Net.Sdk.Signing.SignatureKeyPair.TryImportFromSecretKeyBytes(seed32, signatureType, out var keyPair))
                throw new InvalidOperationException("Failed to import generated key into SignatureKeyPair.");

            return keyPair;
        }

        /// <summary>
        /// Exports the key pair associated with this principal to a base64 string, indicating Lite Token Account type.
        /// </summary>
        /// <returns>Base64 encoded string of the key pair information.</returns>
        public string ExportToBase64()
        {
            // Need access to the protected method in the base class
            return base.ExportToBase64(AccountType.LITE_TOKEN_ACCOUNT);
        }

        /// <summary>
        /// Imports a LiteTokenAccountPrincipal from a base64 encoded key pair string.
        /// Assumes the principal is for the default ACME token URL.
        /// </summary>
        /// <param name="data">The base64 encoded key pair data.</param>
        /// <returns>A new <see cref="LiteTokenAccountPrincipal"/> instance.</returns>
        public static LiteTokenAccountPrincipal ImportFromBase64(string data)
        {
            var keyPair = ImportKeyPairFromBase64(data); // Call protected static method from base
            // Uses the constructor that defaults to the ACME token URL
            return new LiteTokenAccountPrincipal(keyPair);
        }
    }
}

