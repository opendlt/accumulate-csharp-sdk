using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Acme.Net.Sdk.Wallet
{
    /// <summary>
    /// Interface for wallet storage providers that can securely persist wallet data.
    /// </summary>
    public interface IWalletStorage
    {
        /// <summary>
        /// Checks if a wallet with the specified identifier exists in storage.
        /// </summary>
        /// <param name="walletId">The unique identifier of the wallet.</param>
        /// <returns>True if the wallet exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string walletId);
        
        /// <summary>
        /// Saves wallet data to storage.
        /// </summary>
        /// <param name="walletId">The unique identifier of the wallet.</param>
        /// <param name="encryptedData">The encrypted wallet data to save.</param>
        /// <param name="metadata">Optional metadata about the wallet.</param>
        /// <returns>A task that completes when the save operation is finished.</returns>
        Task SaveAsync(string walletId, byte[] encryptedData, Dictionary<string, string>? metadata = null);
        
        /// <summary>
        /// Loads wallet data from storage.
        /// </summary>
        /// <param name="walletId">The unique identifier of the wallet.</param>
        /// <returns>The encrypted wallet data.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet does not exist.</exception>
        Task<byte[]> LoadAsync(string walletId);
        
        /// <summary>
        /// Retrieves metadata for a wallet.
        /// </summary>
        /// <param name="walletId">The unique identifier of the wallet.</param>
        /// <returns>The wallet metadata, or null if no metadata exists.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet does not exist.</exception>
        Task<Dictionary<string, string>?> GetMetadataAsync(string walletId);
        
        /// <summary>
        /// Updates metadata for a wallet.
        /// </summary>
        /// <param name="walletId">The unique identifier of the wallet.</param>
        /// <param name="metadata">The updated metadata.</param>
        /// <returns>A task that completes when the update operation is finished.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the wallet does not exist.</exception>
        Task UpdateMetadataAsync(string walletId, Dictionary<string, string> metadata);
        
        /// <summary>
        /// Deletes a wallet from storage.
        /// </summary>
        /// <param name="walletId">The unique identifier of the wallet.</param>
        /// <returns>A task that completes when the delete operation is finished.</returns>
        Task DeleteAsync(string walletId);
        
        /// <summary>
        /// Lists all wallet IDs in storage.
        /// </summary>
        /// <returns>A list of wallet identifiers.</returns>
        Task<IEnumerable<string>> ListWalletIdsAsync();
    }
} 