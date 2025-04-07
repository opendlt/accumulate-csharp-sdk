using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Signing;
using SignatureKeyPair = Acme.Net.Sdk.Signing.SignatureKeyPair;
using Acme.Net.Sdk.Signing;

namespace Acme.Net.Sdk.Wallet
{
    /// <summary>
    /// Represents a wallet containing multiple accounts with their keys for the Acme network.
    /// </summary>
    public class Wallet : IDisposable
    {
        private readonly IWalletStorage _storage;
        private readonly WalletEncryptionService _encryptionService;
        private readonly Dictionary<string, Account> _accounts = new();
        private readonly JsonSerializerOptions _jsonOptions;
        private string _walletId;
        private string _password;
        private bool _isModified;
        private bool _disposed;
        
        /// <summary>
        /// Gets the wallet identifier.
        /// </summary>
        public string WalletId => _walletId;
        
        /// <summary>
        /// Gets the collection of accounts in this wallet.
        /// </summary>
        public IReadOnlyDictionary<string, Account> Accounts => _accounts;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wallet"/> class.
        /// </summary>
        /// <param name="walletId">The unique identifier for this wallet.</param>
        /// <param name="password">The password to encrypt wallet data.</param>
        /// <param name="storage">The storage provider for wallet data.</param>
        private Wallet(string walletId, string password, IWalletStorage storage)
        {
            if (string.IsNullOrWhiteSpace(walletId))
                throw new ArgumentException("Wallet ID cannot be null or empty", nameof(walletId));
            
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            
            _walletId = walletId;
            _password = password;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _encryptionService = new WalletEncryptionService();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }
        
        /// <summary>
        /// Creates a new wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <param name="password">The password to encrypt wallet data.</param>
        /// <param name="storage">The storage provider for wallet data.</param>
        /// <returns>A new wallet instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a wallet with the specified ID already exists.</exception>
        public static async Task<Wallet> CreateAsync(string walletId, string password, IWalletStorage storage)
        {
            if (await storage.ExistsAsync(walletId))
                throw new InvalidOperationException($"A wallet with ID '{walletId}' already exists");
            
            var wallet = new Wallet(walletId, password, storage);
            wallet._isModified = true; // Mark as modified to ensure it gets saved
            
            // Save the initial empty wallet
            await wallet.SaveAsync();
            
            return wallet;
        }
        
        /// <summary>
        /// Loads an existing wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <param name="password">The password to decrypt wallet data.</param>
        /// <param name="storage">The storage provider for wallet data.</param>
        /// <returns>The loaded wallet instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the wallet data is corrupted or the password is incorrect.</exception>
        public static async Task<Wallet> LoadAsync(string walletId, string password, IWalletStorage storage)
        {
            if (!await storage.ExistsAsync(walletId))
                throw new KeyNotFoundException($"No wallet found with ID '{walletId}'");
            
            var wallet = new Wallet(walletId, password, storage);
            
            try
            {
                // Load wallet data
                byte[] encryptedData = await storage.LoadAsync(walletId);
                await wallet.LoadWalletDataAsync(encryptedData);
                return wallet;
            }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException)
            {
                throw new InvalidOperationException("Failed to decrypt wallet data. The password may be incorrect.", ex);
            }
            catch (Exception ex) when (ex is JsonException)
            {
                throw new InvalidOperationException("Failed to deserialize wallet data. The data may be corrupted.", ex);
            }
        }
        
        /// <summary>
        /// Adds an account to the wallet.
        /// </summary>
        /// <param name="account">The account to add.</param>
        /// <returns>This wallet instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if account is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an account with the same URL already exists.</exception>
        public Wallet AddAccount(Account account)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            
            string accountKey = account.Url.ToString();
            
            if (_accounts.ContainsKey(accountKey))
                throw new InvalidOperationException($"An account with URL '{accountKey}' already exists in this wallet");
            
            _accounts[accountKey] = account;
            _isModified = true;
            
            return this;
        }
        
        /// <summary>
        /// Creates a new account with a generated key and adds it to the wallet.
        /// </summary>
        /// <param name="url">The account URL.</param>
        /// <param name="type">The account type.</param>
        /// <param name="keyId">Optional key ID for the generated key.</param>
        /// <returns>The created account.</returns>
        public Account CreateAccount(Url url, AccountType type, string keyId = "main")
        {
            var account = Account.CreateWithNewKey(url, type, keyId);
            AddAccount(account);
            return account;
        }
        
        /// <summary>
        /// Removes an account from the wallet.
        /// </summary>
        /// <param name="url">The URL of the account to remove.</param>
        /// <returns>True if the account was removed, false if not found.</returns>
        public bool RemoveAccount(Url url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            
            string accountKey = url.ToString();
            
            if (!_accounts.ContainsKey(accountKey))
                return false;
            
            _accounts.Remove(accountKey);
            _isModified = true;
            
            return true;
        }
        
        /// <summary>
        /// Gets an account by its URL.
        /// </summary>
        /// <param name="url">The account URL.</param>
        /// <returns>The account if found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the account is not found.</exception>
        public Account GetAccount(Url url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            
            string accountKey = url.ToString();
            
            if (!_accounts.TryGetValue(accountKey, out var account))
                throw new KeyNotFoundException($"Account with URL '{accountKey}' not found in this wallet");
            
            return account;
        }
        
        /// <summary>
        /// Checks if an account with the specified URL exists in this wallet.
        /// </summary>
        /// <param name="url">The account URL to check.</param>
        /// <returns>True if the account exists, false otherwise.</returns>
        public bool ContainsAccount(Url url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            
            string accountKey = url.ToString();
            return _accounts.ContainsKey(accountKey);
        }
        
        /// <summary>
        /// Changes the wallet password.
        /// </summary>
        /// <param name="newPassword">The new password.</param>
        /// <exception cref="ArgumentException">Thrown if the new password is invalid.</exception>
        public void ChangePassword(string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentException("New password cannot be null or empty", nameof(newPassword));
            
            _password = newPassword;
            _isModified = true;
        }
        
        /// <summary>
        /// Saves the wallet to storage.
        /// </summary>
        /// <returns>A task that completes when the save operation is finished.</returns>
        public async Task SaveAsync()
        {
            if (!_isModified)
                return;
            
            var data = SerializeWalletData();
            var encryptedData = _encryptionService.Encrypt(data, _password);
            
            // Update metadata with basic info
            var metadata = new Dictionary<string, string>
            {
                { "version", "1.0" },
                { "accountCount", _accounts.Count.ToString() }
            };
            
            await _storage.SaveAsync(_walletId, encryptedData, metadata);
            _isModified = false;
        }
        
        /// <summary>
        /// Loads wallet data from encrypted bytes.
        /// </summary>
        /// <param name="encryptedData">The encrypted wallet data.</param>
        /// <returns>A task that completes when the load operation is finished.</returns>
        private async Task LoadWalletDataAsync(byte[] encryptedData)
        {
            byte[] decryptedData = _encryptionService.Decrypt(encryptedData, _password);
            DeserializeWalletData(decryptedData);
            _isModified = false;
        }
        
        /// <summary>
        /// Serializes wallet data to a byte array.
        /// </summary>
        /// <returns>The serialized wallet data.</returns>
        private byte[] SerializeWalletData()
        {
            var walletData = new WalletData
            {
                Accounts = _accounts.Values.Select(a => new AccountData
                {
                    Url = a.Url.ToString(),
                    Type = a.Type,
                    DefaultKeyId = a.DefaultKeyId,
                    Keys = a.KeyIds.ToDictionary(
                        keyId => keyId,
                        keyId => new KeyData
                        {
                            Type = a.GetKeyPair(keyId).Type,
                            PrivateKey = Convert.ToBase64String(a.GetKeyPair(keyId).GetPrivateKeyBytes())
                        }
                    )
                }).ToList()
            };
            
            string json = JsonSerializer.Serialize(walletData, _jsonOptions);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        
        /// <summary>
        /// Deserializes wallet data from a byte array.
        /// </summary>
        /// <param name="data">The serialized wallet data.</param>
        private void DeserializeWalletData(byte[] data)
        {
            _accounts.Clear();
            
            if (data == null || data.Length == 0)
                return;
            
            string json = System.Text.Encoding.UTF8.GetString(data);
            var walletData = JsonSerializer.Deserialize<WalletData>(json, _jsonOptions);
            
            if (walletData == null || walletData.Accounts == null)
                return;
                
            // Rebuild accounts
            foreach (var accountData in walletData.Accounts)
            {
                var url = new Url(accountData.Url);
                var account = new Account(url, accountData.Type);
                
                foreach (var keyEntry in accountData.Keys)
                {
                    string keyId = keyEntry.Key;
                    var keyData = keyEntry.Value;
                    
                    byte[] privateKeyBytes = Convert.FromBase64String(keyData.PrivateKey);
                    bool result = SignatureKeyPair.TryImportFromSecretKeyBytes(privateKeyBytes, keyData.Type, out var keyPair);
                    
                    if (result && keyPair != null)
                    {
                        account.AddKeyPair(keyId, keyPair, keyId == accountData.DefaultKeyId);
                    }
                }
                
                _accounts[accountData.Url] = account;
            }
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_storage is IDisposable disposableStorage)
                {
                    disposableStorage.Dispose();
                }
                
                // Clear sensitive data
                _password = null;
                _accounts.Clear();
                
                _disposed = true;
            }
        }
        
        #region Helper Classes
        
        private class WalletData
        {
            public List<AccountData> Accounts { get; set; } = new List<AccountData>();
        }
        
        private class AccountData
        {
            public string Url { get; set; }
            public AccountType Type { get; set; }
            public string DefaultKeyId { get; set; }
            public Dictionary<string, KeyData> Keys { get; set; } = new Dictionary<string, KeyData>();
        }
        
        private class KeyData
        {
            public SignatureType Type { get; set; }
            public string PrivateKey { get; set; } // Base64 encoded private key
        }
        
        #endregion
    }
} 