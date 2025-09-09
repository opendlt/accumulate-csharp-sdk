using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using NSec.Cryptography; // For crypto operations
using Acme.Net.Sdk.Protocol.Generated; // Reverted to correct namespace for AccountType, SignatureType
using Acme.Net.Sdk.Protocol; // Keep for Url, IAccount etc.
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex
using Acme.Net.Sdk.Support; // For HashUtils
using Acme.Net.Sdk.Signing; // For the centralized SignatureKeyPair

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Represents a principal (account and associated key pair) used for signing transactions.
    /// Corresponds to io.accumulatenetwork.sdk.protocol.Principal.
    /// </summary>
    public class Principal
    {
        private readonly IAccount _account;
        private readonly Acme.Net.Sdk.Signing.SignatureKeyPair _signatureKeyPair;
        private int _signerVersion = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Principal"/> class.
        /// </summary>
        /// <param name="account">The account associated with the principal.</param>
        /// <param name="signatureKeyPair">The key pair used for signing.</param>
        /// <exception cref="ArgumentNullException">Thrown if account or signatureKeyPair is null.</exception>
        public Principal(IAccount account, Acme.Net.Sdk.Signing.SignatureKeyPair signatureKeyPair)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _signatureKeyPair = signatureKeyPair ?? throw new ArgumentNullException(nameof(signatureKeyPair));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Principal"/> class with a specific signer version.
        /// </summary>
        /// <param name="account">The account associated with the principal.</param>
        /// <param name="signatureKeyPair">The key pair used for signing.</param>
        /// <param name="signerVersion">The version of the signer.</param>
        /// <exception cref="ArgumentNullException">Thrown if account or signatureKeyPair is null.</exception>
        public Principal(IAccount account, Acme.Net.Sdk.Signing.SignatureKeyPair signatureKeyPair, int signerVersion)
            : this(account, signatureKeyPair)
        {
            _signerVersion = signerVersion;
        }

        /// <summary>
        /// Protected constructor for derived classes or internal use.
        /// </summary>
        protected Principal()
        {
            // Default constructor needs initialization via properties or other means
            _account = null!; 
            _signatureKeyPair = null!;
        }

        /// <summary>
        /// Gets the account associated with this principal.
        /// </summary>
        public IAccount Account => _account;

        /// <summary>
        /// Gets the signature key pair associated with this principal.
        /// </summary>
        public Acme.Net.Sdk.Signing.SignatureKeyPair SignatureKeyPair => _signatureKeyPair;

        /// <summary>
        /// Gets or sets the signer version.
        /// </summary>
        public int SignerVersion
        {
            get => _signerVersion;
            set => _signerVersion = value;
        }

        /// <summary>
        /// Exports the principal's key pair information (type and private key) to a Base64 encoded string.
        /// </summary>
        /// <param name="accountType">The type of account associated with this principal.</param>
        /// <returns>A Base64 encoded string representing the key pair information.</returns>
        /// <exception cref="IOException">Thrown if an error occurs during stream writing.</exception>
        /// <exception cref="NotSupportedException">Thrown if the private key cannot be exported.</exception>
        protected string ExportToBase64(AccountType accountType)
        {
            using var memoryStream = new MemoryStream();
            // Use BinaryWriter for primitive types - assumes BigEndian is NOT required based on Java DataOutputStream defaults
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, false)) 
            {
                writer.Write((byte)accountType); // Write account type ordinal
                // Correctly access SignatureType from the SignatureKeyPair instance
                writer.Write((byte)_signatureKeyPair.Type); 
                
                // Use the new internal method to get the private key bytes
                byte[] privateKey = _signatureKeyPair.GetPrivateKeyBytes(); 
                if (privateKey.Length > byte.MaxValue) 
                {
                     throw new InvalidOperationException("Private key length exceeds maximum representable value (255).");
                }
                writer.Write((byte)privateKey.Length); // Write private key length
                writer.Write(privateKey); // Write private key bytes
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        /// <summary>
        /// Imports a signature key pair from a Base64 encoded string.
        /// </summary>
        /// <param name="data">The Base64 encoded key pair data.</param>
        /// <returns>A <see cref="SignatureKeyPair"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
        /// <exception cref="FormatException">Thrown if the Base64 string is invalid.</exception>
        /// <exception cref="IOException">Thrown if an error occurs during stream reading.</exception>
        /// <exception cref="ArgumentException">Thrown if the read key bytes are invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if the signature type is not supported.</exception>
        protected static Acme.Net.Sdk.Signing.SignatureKeyPair ImportKeyPairFromBase64(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            byte[] decodedBytes = Convert.FromBase64String(data);
            using var memoryStream = new MemoryStream(decodedBytes);
            using var reader = new BinaryReader(memoryStream, Encoding.UTF8, false);
            
            reader.ReadByte(); // Skip account type byte
            byte sigTypeValue = reader.ReadByte();
            SignatureType signatureType = SignatureTypeExtensions.FromValue(sigTypeValue);
            if (signatureType == SignatureType.UNKNOWN && sigTypeValue != 0)
            {
                 throw new FormatException($"Invalid signature type value read from data: {sigTypeValue}");
            }
            
            byte keyLength = reader.ReadByte();
            byte[] secretKey = reader.ReadBytes(keyLength);

            // Check if we read the expected number of bytes for the key
             if(secretKey.Length != keyLength) {
                 throw new IOException("Could not read the expected number of bytes for the secret key.");
             }

            // Create KeyPair using the factory method in SignatureKeyPair
            // Use the fully qualified name for the static method
            if (Acme.Net.Sdk.Signing.SignatureKeyPair.TryImportFromSecretKeyBytes(secretKey, signatureType, out var importedKeyPair))
            {
                return importedKeyPair;
            }
            else
            {
                // Throw a more specific exception if import fails
                throw new ArgumentException($"Failed to import secret key bytes. Ensure the key format ({secretKey.Length} bytes) is correct for signature type {signatureType}.", nameof(data));
            }
        }

        /// <summary>
        /// Computes a lite identity URL from a public key.
        /// </summary>
        /// <param name="publicKey">The public key bytes.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        public static Url ComputeUrl(byte[] publicKey)
        {
            return ComputeUrl(publicKey, null);
        }

        /// <summary>
        /// Computes a lite identity URL from a public key, optionally merging with another URL.
        /// </summary>
        /// <param name="publicKey">The public key bytes.</param>
        /// <param name="mergeUrl">An optional URL whose authority should be appended.</param>
        /// <returns>The computed <see cref="Url"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if publicKey is null.</exception>
        public static Url ComputeUrl(byte[] publicKey, Url? mergeUrl)
        {
             if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));

            byte[] hash = HashUtils.Sha256(publicKey);
            // Take first 20 bytes of the hash
            string pkHash = Convert.ToHexString(hash[0..20]).ToLowerInvariant(); // Use range operator and ensure lower case hex
            
            // Compute checksum (SHA256 of the hex string of the first 20 bytes)
            byte[] checkSumInputBytes = Encoding.UTF8.GetBytes(pkHash); // Get bytes of the hex string
            byte[] checkSum = HashUtils.Sha256(checkSumInputBytes);
            // Take last 4 bytes of the checksum hash
            string checkSumStr = Convert.ToHexString(checkSum[28..32]).ToLowerInvariant(); // Use range operator
            
            var urlBuilder = new StringBuilder("acc://").Append(pkHash).Append(checkSumStr);
            if (mergeUrl != null)
            {
                // In Java, the authority() method returns the URI.getHost(), not the Authority property
                // This is why we need to use HostName instead of Authority
                urlBuilder.Append('/').Append(mergeUrl.HostName);
            }
            
            // Use Url.Parse which handles acc:// prefixing if needed (though already added here)
            return Url.Parse(urlBuilder.ToString());
        }
    }
}
