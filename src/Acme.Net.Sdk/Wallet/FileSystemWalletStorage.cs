using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Acme.Net.Sdk.Wallet
{
    /// <summary>
    /// A file system implementation of <see cref="IWalletStorage"/> that saves encrypted wallet data to disk.
    /// </summary>
    public class FileSystemWalletStorage : IWalletStorage, IDisposable
    {
        private readonly string _baseDirectory;
        private readonly object _lockObj = new object();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemWalletStorage"/> class.
        /// </summary>
        /// <param name="baseDirectory">The base directory where wallet data will be stored.</param>
        public FileSystemWalletStorage(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory cannot be null or empty", nameof(baseDirectory));

            _baseDirectory = baseDirectory;
            
            // Ensure the directory exists
            Directory.CreateDirectory(_baseDirectory);
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "wallets"));
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string walletId)
        {
            ValidateWalletId(walletId);
            string walletFilePath = GetWalletFilePath(walletId);
            string metadataFilePath = GetMetadataFilePath(walletId);
            
            // Check if both files exist
            bool walletExists = File.Exists(walletFilePath);
            bool metadataExists = File.Exists(metadataFilePath);
            
            return walletExists && metadataExists;
        }

        /// <inheritdoc/>
        public async Task SaveAsync(string walletId, byte[] encryptedData, Dictionary<string, string>? metadata = null)
        {
            ValidateWalletId(walletId);
            
            if (encryptedData == null || encryptedData.Length == 0)
                throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

            string walletFilePath = GetWalletFilePath(walletId);
            string metadataFilePath = GetMetadataFilePath(walletId);
            
            // Create the wallet directory if it doesn't exist
            string walletsDirectory = Path.Combine(_baseDirectory, "wallets");
            if (!Directory.Exists(walletsDirectory))
            {
                Directory.CreateDirectory(walletsDirectory);
            }
            
            // Save encrypted wallet data
            await File.WriteAllBytesAsync(walletFilePath, encryptedData);
            
            // Save metadata
            metadata ??= new Dictionary<string, string>();
            metadata["lastUpdated"] = DateTimeOffset.UtcNow.ToString("o");
            
            string metadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
            await File.WriteAllTextAsync(metadataFilePath, metadataJson);
        }

        /// <inheritdoc/>
        public async Task<byte[]> LoadAsync(string walletId)
        {
            ValidateWalletId(walletId);
            string walletFilePath = GetWalletFilePath(walletId);
            
            if (!File.Exists(walletFilePath))
                throw new KeyNotFoundException($"Wallet with ID '{walletId}' not found");
            
            return await File.ReadAllBytesAsync(walletFilePath);
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>?> GetMetadataAsync(string walletId)
        {
            ValidateWalletId(walletId);
            string metadataFilePath = GetMetadataFilePath(walletId);
            
            if (!File.Exists(metadataFilePath))
                throw new KeyNotFoundException($"Metadata for wallet with ID '{walletId}' not found");

            string metadataJson = await File.ReadAllTextAsync(metadataFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
        }

        /// <inheritdoc/>
        public async Task UpdateMetadataAsync(string walletId, Dictionary<string, string> metadata)
        {
            ValidateWalletId(walletId);
            
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            
            string metadataFilePath = GetMetadataFilePath(walletId);
            
            if (!File.Exists(metadataFilePath))
                throw new KeyNotFoundException($"Metadata for wallet with ID '{walletId}' not found");

            metadata["lastUpdated"] = DateTimeOffset.UtcNow.ToString("o");
            string metadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
            await File.WriteAllTextAsync(metadataFilePath, metadataJson);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string walletId)
        {
            ValidateWalletId(walletId);
            string walletFilePath = GetWalletFilePath(walletId);
            string metadataFilePath = GetMetadataFilePath(walletId);
            
            // Delete files if they exist
            if (File.Exists(walletFilePath))
                File.Delete(walletFilePath);
            
            if (File.Exists(metadataFilePath))
                File.Delete(metadataFilePath);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> ListWalletIdsAsync()
        {
            string walletsDirectory = Path.Combine(_baseDirectory, "wallets");
            
            if (!Directory.Exists(walletsDirectory))
                return Enumerable.Empty<string>();
            
            return Directory.GetFiles(walletsDirectory, "*.wallet")
                .Select(Path.GetFileNameWithoutExtension);
        }
        
        private string GetWalletFilePath(string walletId)
        {
            return Path.Combine(_baseDirectory, "wallets", $"{walletId}.wallet");
        }
        
        private string GetMetadataFilePath(string walletId)
        {
            return Path.Combine(_baseDirectory, "wallets", $"{walletId}.metadata");
        }
        
        private void ValidateWalletId(string walletId)
        {
            if (string.IsNullOrWhiteSpace(walletId))
                throw new ArgumentException("Wallet ID cannot be null or empty", nameof(walletId));
            
            if (walletId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Wallet ID contains invalid characters", nameof(walletId));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Perform any cleanup if needed
                _disposed = true;
            }
        }
    }
} 