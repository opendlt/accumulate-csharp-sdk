using System;
using System.Collections.Generic;
using System.Linq;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Signing;
using SignatureKeyPair = Acme.Net.Sdk.Signing.SignatureKeyPair;
using Acme.Net.Sdk.Signing;

namespace Acme.Net.Sdk.Wallet
{
    /// <summary>
    /// Represents an account in the Acme network with key management and transaction signing capabilities.
    /// </summary>
    public class Account
    {
        private readonly Dictionary<string, SignatureKeyPair> _keyPairs = new();
        private readonly TransactionSignerService _signerService = new();

        /// <summary>
        /// Gets the account URL.
        /// </summary>
        public Url Url { get; }

        /// <summary>
        /// Gets the account name (derived from URL).
        /// </summary>
        public string Name => Url.ToString().Split('/').Last().Split('.').First();

        /// <summary>
        /// Gets the account type.
        /// </summary>
        public AccountType Type { get; }

        /// <summary>
        /// Gets a list of key IDs in this account.
        /// </summary>
        public IReadOnlyList<string> KeyIds => _keyPairs.Keys.ToList();

        /// <summary>
        /// Gets or sets the default key ID to use for signing operations.
        /// </summary>
        public string? DefaultKeyId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        /// <param name="url">The account URL.</param>
        /// <param name="type">The account type.</param>
        public Account(Url url, AccountType type)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            Type = type;
        }

        /// <summary>
        /// Adds a key pair to the account with the specified ID.
        /// </summary>
        /// <param name="keyId">The key ID to use for this key pair.</param>
        /// <param name="keyPair">The signature key pair to add.</param>
        /// <param name="setAsDefault">Whether to set this as the default key for signing.</param>
        /// <returns>This account instance for chaining.</returns>
        public Account AddKeyPair(string keyId, SignatureKeyPair keyPair, bool setAsDefault = false)
        {
            if (string.IsNullOrWhiteSpace(keyId))
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            
            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));
            
            if (_keyPairs.ContainsKey(keyId))
                throw new InvalidOperationException($"A key with ID '{keyId}' already exists");
            
            _keyPairs[keyId] = keyPair;
            
            if (setAsDefault || DefaultKeyId == null)
                DefaultKeyId = keyId;
            
            return this;
        }

        /// <summary>
        /// Gets a key pair by its ID.
        /// </summary>
        /// <param name="keyId">The key ID to retrieve, or null to use the default key.</param>
        /// <returns>The signature key pair.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key ID is not found or no default key is set.</exception>
        public SignatureKeyPair GetKeyPair(string? keyId = null)
        {
            keyId ??= DefaultKeyId;
            
            if (keyId == null)
                throw new InvalidOperationException("No default key is set and no key ID was provided");
            
            if (!_keyPairs.TryGetValue(keyId, out var keyPair))
                throw new KeyNotFoundException($"Key with ID '{keyId}' not found");
            
            return keyPair;
        }

        /// <summary>
        /// Removes a key pair by its ID.
        /// </summary>
        /// <param name="keyId">The key ID to remove.</param>
        /// <returns>True if the key was removed, false if it wasn't found.</returns>
        public bool RemoveKeyPair(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId))
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            
            if (!_keyPairs.ContainsKey(keyId))
                return false;
            
            _keyPairs.Remove(keyId);
            
            // If we removed the default key, set a new default if available
            if (keyId == DefaultKeyId)
            {
                DefaultKeyId = _keyPairs.Keys.FirstOrDefault();
            }
            
            return true;
        }

        /// <summary>
        /// Signs a transaction using a key from this account.
        /// </summary>
        /// <param name="transaction">The transaction to sign.</param>
        /// <param name="keyId">The key ID to use for signing, or null to use the default key.</param>
        /// <returns>The transaction signature.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key ID is not found or no default key is set.</exception>
        public TransactionSignature SignTransaction(Transaction transaction, string? keyId = null)
        {
            var keyPair = GetKeyPair(keyId);
            string signatureType = keyPair.Type.ToString().ToLowerInvariant();
            
            // Get private key bytes for signing
            byte[] privateKeySeed = keyPair.GetPrivateKeyBytes();
            
            // Sign the transaction using the appropriate signer
            return _signerService.SignTransaction(transaction, signatureType, privateKeySeed);
        }

        /// <summary>
        /// Creates a new account with a generated ED25519 key.
        /// </summary>
        /// <param name="url">The account URL.</param>
        /// <param name="type">The account type.</param>
        /// <param name="keyId">Optional ID for the generated key, defaults to "main".</param>
        /// <returns>A new account with a generated key.</returns>
        public static Account CreateWithNewKey(Url url, AccountType type, string keyId = "main")
        {
            // Use the Ed25519SignatureKeyPair constructor directly to ensure exportable key
            var keyPair = new Ed25519SignatureKeyPair();
            var account = new Account(url, type);
            account.AddKeyPair(keyId, keyPair, true);
            return account;
        }
    }

    /// <summary>
    /// Represents the type of an account in the Acme network.
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// A lite token account.
        /// </summary>
        LiteTokenAccount,
        
        /// <summary>
        /// A regular token account.
        /// </summary>
        TokenAccount,
        
        /// <summary>
        /// A lite data account.
        /// </summary>
        LiteDataAccount,
        
        /// <summary>
        /// A regular data account.
        /// </summary>
        DataAccount,
        
        /// <summary>
        /// An ADI (Accumulate Digital Identifier) account.
        /// </summary>
        ADI,
        
        /// <summary>
        /// A key book account.
        /// </summary>
        KeyBook,
        
        /// <summary>
        /// A key page account.
        /// </summary>
        KeyPage,
        
        /// <summary>
        /// An unknown account type.
        /// </summary>
        Unknown
    }
} 