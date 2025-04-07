using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Acme.Net.Sdk.Wallet
{
    /// <summary>
    /// Provides centralized management of wallets for the Acme network.
    /// </summary>
    public class WalletManager : IDisposable
    {
        private readonly IWalletStorage _storage;
        private readonly Dictionary<string, Wallet> _openWallets = new();
        private bool _disposed;
        
        /// <summary>
        /// Gets the default wallet directory.
        /// </summary>
        public static string DefaultWalletDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Acme.Net.Sdk",
            "Wallets");
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WalletManager"/> class with a file system storage.
        /// </summary>
        /// <param name="walletDirectory">Optional custom directory for wallet storage.</param>
        public WalletManager(string? walletDirectory = null)
            : this(new FileSystemWalletStorage(walletDirectory ?? DefaultWalletDirectory))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WalletManager"/> class with a custom storage provider.
        /// </summary>
        /// <param name="storage">The wallet storage provider.</param>
        public WalletManager(IWalletStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }
        
        /// <summary>
        /// Creates a new wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <param name="password">The password to encrypt wallet data.</param>
        /// <returns>The created wallet.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a wallet with the specified ID already exists.</exception>
        public async Task<Wallet> CreateWalletAsync(string walletId, string password)
        {
            var wallet = await Wallet.CreateAsync(walletId, password, _storage);
            _openWallets[walletId] = wallet;
            return wallet;
        }
        
        /// <summary>
        /// Loads an existing wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <param name="password">The password to decrypt wallet data.</param>
        /// <returns>The loaded wallet.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the wallet data is corrupted or the password is incorrect.</exception>
        public async Task<Wallet> LoadWalletAsync(string walletId, string password)
        {
            // If the wallet is already open, return it
            if (_openWallets.TryGetValue(walletId, out var openWallet))
            {
                return openWallet;
            }
            
            var wallet = await Wallet.LoadAsync(walletId, password, _storage);
            _openWallets[walletId] = wallet;
            return wallet;
        }
        
        /// <summary>
        /// Closes an open wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <param name="save">Whether to save changes before closing.</param>
        /// <returns>A task that completes when the close operation is finished.</returns>
        public async Task CloseWalletAsync(string walletId, bool save = true)
        {
            if (!_openWallets.TryGetValue(walletId, out var wallet))
                return;
            
            if (save)
            {
                await wallet.SaveAsync();
            }
            
            if (wallet is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _openWallets.Remove(walletId);
        }
        
        /// <summary>
        /// Checks if a wallet exists.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <returns>True if the wallet exists, false otherwise.</returns>
        public async Task<bool> WalletExistsAsync(string walletId)
        {
            return await _storage.ExistsAsync(walletId);
        }
        
        /// <summary>
        /// Deletes a wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <returns>A task that completes when the delete operation is finished.</returns>
        public async Task DeleteWalletAsync(string walletId)
        {
            await CloseWalletAsync(walletId, false);
            await _storage.DeleteAsync(walletId);
        }
        
        /// <summary>
        /// Lists all wallet IDs in storage.
        /// </summary>
        /// <returns>A list of wallet identifiers.</returns>
        public async Task<IEnumerable<string>> ListWalletIdsAsync()
        {
            return await _storage.ListWalletIdsAsync();
        }
        
        /// <summary>
        /// Gets metadata for a wallet.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <returns>The wallet metadata, or null if no metadata exists.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet is not found.</exception>
        public async Task<Dictionary<string, string>?> GetWalletMetadataAsync(string walletId)
        {
            return await _storage.GetMetadataAsync(walletId);
        }
        
        /// <summary>
        /// Gets an open wallet by its ID.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <returns>The wallet if it's open.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet is not open.</exception>
        public Wallet GetOpenWallet(string walletId)
        {
            if (!_openWallets.TryGetValue(walletId, out var wallet))
                throw new KeyNotFoundException($"No open wallet found with ID '{walletId}'");
            
            return wallet;
        }
        
        /// <summary>
        /// Checks if a wallet is currently open.
        /// </summary>
        /// <param name="walletId">The wallet identifier.</param>
        /// <returns>True if the wallet is open, false otherwise.</returns>
        public bool IsWalletOpen(string walletId)
        {
            return _openWallets.ContainsKey(walletId);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Close and dispose all open wallets
                foreach (var wallet in _openWallets.Values)
                {
                    if (wallet is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                _openWallets.Clear();
                
                if (_storage is IDisposable disposableStorage)
                {
                    disposableStorage.Dispose();
                }
                
                _disposed = true;
            }
        }
    }
} 