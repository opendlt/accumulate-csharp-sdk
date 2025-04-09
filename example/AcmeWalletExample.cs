using System;
using System.Threading.Tasks;
using System.IO;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Wallet;
using System.Numerics;

namespace AcmeWalletExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Acme.Net SDK Wallet Example");
            Console.WriteLine("===========================");

            try
            {
                // Create a temporary directory for wallet storage
                string tempWalletDir = Path.Combine(Path.GetTempPath(), "AcmeWalletExample");
                Directory.CreateDirectory(tempWalletDir);
                Console.WriteLine($"Wallet files will be stored in: {tempWalletDir}");

                // Create a wallet manager with the temporary directory
                using var walletManager = new WalletManager(tempWalletDir);

                // Create a new wallet
                string walletId = "my-wallet";
                string password = "secure-password";

                if (await walletManager.WalletExistsAsync(walletId))
                {
                    Console.WriteLine($"Wallet '{walletId}' already exists. Loading...");
                    await walletManager.LoadWalletAsync(walletId, password);
                }
                else
                {
                    Console.WriteLine($"Creating new wallet: {walletId}");
                    await walletManager.CreateWalletAsync(walletId, password);
                }

                // Get the open wallet
                var wallet = walletManager.GetOpenWallet(walletId);

                // Create a client for Acme network interactions
                var client = new AcmeClient("https://testnet.accumulatenetwork.io/v2");

                // Generate accounts with different types
                Console.WriteLine("\nGenerating accounts in wallet...");

                // Create a lite identity account
                var liteIdentityUrl = GenerateLiteIdentityUrl();
                var liteIdentity = await CreateAccount(wallet, liteIdentityUrl, AccountType.ADI);
                Console.WriteLine($"Created Lite Identity account: {liteIdentity.Url}");

                // Create token account
                var acmeTokenUrl = UrlRegistry.GetInstance().GetAcmeTokenUrl();
                var tokenAccountUrl = new Url($"{liteIdentity.Url}/acme");
                var tokenAccount = await CreateAccount(wallet, tokenAccountUrl, AccountType.TokenAccount);
                Console.WriteLine($"Created Token account: {tokenAccount.Url}");

                // Create data account
                var dataAccountUrl = new Url($"{liteIdentity.Url}/data");
                var dataAccount = await CreateAccount(wallet, dataAccountUrl, AccountType.DataAccount);
                Console.WriteLine($"Created Data account: {dataAccount.Url}");

                // List all accounts in wallet
                Console.WriteLine("\nAccounts in wallet:");
                foreach (var account in wallet.Accounts.Values)
                {
                    Console.WriteLine($"- {account.Url} (Type: {account.Type})");
                    Console.WriteLine($"  Key IDs: {string.Join(", ", account.KeyIds)}");
                    Console.WriteLine($"  Default Key: {account.DefaultKeyId}");
                }

                // Save the wallet to storage
                await wallet.SaveAsync();
                Console.WriteLine("\nWallet saved to storage.");

                // Example transaction: Create token account on blockchain
                Console.WriteLine("\nCreating token account on blockchain...");
                await CreateTokenAccountOnChain(client, liteIdentity, tokenAccount, acmeTokenUrl);

                // Example transaction: Write data to blockchain
                Console.WriteLine("\nWriting data to blockchain...");
                await WriteDataToBlockchain(client, liteIdentity, dataAccount);

                // Example transaction: Send tokens
                Console.WriteLine("\nSending tokens...");
                var recipient = GenerateLiteIdentityUrl();
                await SendTokens(client, liteIdentity, tokenAccount, recipient);

                // Close the wallet
                await walletManager.CloseWalletAsync(walletId);
                Console.WriteLine("\nWallet closed.");

                // Demonstrate wallet backup and import
                Console.WriteLine("\nDemonstrating wallet backup and restore...");
                await BackupAndRestoreWallet(walletManager, walletId, password);

                Console.WriteLine("\nExample completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static Url GenerateLiteIdentityUrl()
        {
            // Generate a random ED25519 key pair
            var keyPair = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            
            // Compute a Lite Identity URL from the public key
            return Principal.ComputeUrl(keyPair.GetPublicKey());
        }

        static async Task<Account> CreateAccount(Wallet wallet, Url url, AccountType type)
        {
            // Check if account already exists
            if (wallet.ContainsAccount(url))
            {
                return wallet.GetAccount(url);
            }

            // Create a new account with a generated key
            var account = wallet.CreateAccount(url, type);
            return account;
        }

        static async Task CreateTokenAccountOnChain(AcmeClient client, Account identity, Account tokenAccount, Url tokenUrl)
        {
            try
            {
                // Use the identity account to sign the transaction
                var transaction = client.CreateTokenAccountBuilder()
                    .WithOrigin(identity.Url)
                    .WithAccountUrl(tokenAccount.Url)
                    .WithTokenUrl(tokenUrl);

                // Create a signing service
                var signingService = new SigningService(transaction);
                
                // Add the identity account as a signer
                signingService.AddSigner(identity);
                
                // Execute the transaction
                var response = await signingService.ExecuteAsync();
                
                if (string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Token account creation submitted. Transaction ID: {response.TxId}");
                }
                else
                {
                    Console.WriteLine($"Error creating token account: {response.Error}");
                }
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"RPC Error: {ex.Message}");
            }
        }

        static async Task WriteDataToBlockchain(AcmeClient client, Account identity, Account dataAccount)
        {
            try
            {
                // Prepare data to write
                string message = $"Hello from Acme.Net.Sdk Wallet at {DateTime.UtcNow}";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                
                // Create a transaction to write data
                var transaction = client.WriteDataToBuilder()
                    .WithOrigin(identity.Url)
                    .WithRecipient(dataAccount.Url)
                    .WithData(data);
                
                // Create a signing service
                var signingService = new SigningService(transaction);
                
                // Add the identity account as a signer
                signingService.AddSigner(identity);
                
                // Execute the transaction
                var response = await signingService.ExecuteAsync();
                
                if (string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Data written. Transaction ID: {response.TxId}");
                    Console.WriteLine($"Message: {message}");
                }
                else
                {
                    Console.WriteLine($"Error writing data: {response.Error}");
                }
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"RPC Error: {ex.Message}");
            }
        }

        static async Task SendTokens(AcmeClient client, Account identity, Account tokenAccount, Url recipient)
        {
            try
            {
                // Create recipient token account URL
                var recipientTokenUrl = new Url($"{recipient}/acme");
                
                // Build the send tokens transaction
                var transaction = client.CreateSendTokensBuilder()
                    .WithOrigin(identity.Url)
                    .WithSourceUrl(tokenAccount.Url)
                    .WithRecipientUrl(recipientTokenUrl)
                    .WithAmount(BigInteger.Parse("1000000")); // 0.01 ACME (assuming 8 decimal places)
                
                // Create a signing service
                var signingService = new SigningService(transaction);
                
                // Add the identity account as a signer
                signingService.AddSigner(identity);
                
                // Execute the transaction
                var response = await signingService.ExecuteAsync();
                
                if (string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Tokens sent. Transaction ID: {response.TxId}");
                    Console.WriteLine($"From: {tokenAccount.Url}");
                    Console.WriteLine($"To: {recipientTokenUrl}");
                    Console.WriteLine($"Amount: 0.01 ACME");
                }
                else
                {
                    Console.WriteLine($"Error sending tokens: {response.Error}");
                }
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"RPC Error: {ex.Message}");
            }
        }

        static async Task BackupAndRestoreWallet(WalletManager walletManager, string walletId, string password)
        {
            // Load the original wallet
            var originalWallet = await walletManager.LoadWalletAsync(walletId, password);
            
            // Create a backup wallet ID
            string backupWalletId = $"{walletId}-backup";
            
            // Create a new wallet for the backup
            var backupWallet = await walletManager.CreateWalletAsync(backupWalletId, password);
            Console.WriteLine($"Created backup wallet: {backupWalletId}");
            
            // Copy accounts from original to backup
            foreach (var account in originalWallet.Accounts.Values)
            {
                // For each account in the original wallet
                Console.WriteLine($"Copying account: {account.Url}");
                
                // Create the same account in the backup wallet
                var newAccount = backupWallet.CreateAccount(account.Url, account.Type);
                
                // Set the same default key
                newAccount.DefaultKeyId = account.DefaultKeyId;
                
                Console.WriteLine($"- Account copied: {newAccount.Url}");
            }
            
            // Save the backup wallet
            await backupWallet.SaveAsync();
            Console.WriteLine("Backup wallet saved.");
            
            // Close both wallets
            await walletManager.CloseWalletAsync(walletId);
            await walletManager.CloseWalletAsync(backupWalletId);
            
            // List all wallet IDs in storage
            var walletIds = await walletManager.ListWalletIdsAsync();
            Console.WriteLine("\nWallets in storage:");
            foreach (var id in walletIds)
            {
                var metadata = await walletManager.GetWalletMetadataAsync(id);
                string lastUpdated = metadata?.ContainsKey("lastUpdated") == true ? metadata["lastUpdated"] : "unknown";
                Console.WriteLine($"- {id} (Last updated: {lastUpdated})");
            }
        }
    }

    // Helper class to simplify transaction signing using wallet accounts
    class SigningService
    {
        private readonly TransactionBuilder _transaction;
        
        public SigningService(TransactionBuilder transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }
        
        public void AddSigner(Account account)
        {
            // Add a custom signer implementation that uses the account's signing method
            _transaction.WithSigner(new AccountSigner(account));
        }
        
        public async Task<TxResponse> ExecuteAsync()
        {
            return await _transaction.ExecuteSignedAsync();
        }
        
        // Inner class that adapts an Account to be a Signer
        private class AccountSigner : Signer
        {
            private readonly Account _account;
            
            public AccountSigner(Account account)
            {
                _account = account ?? throw new ArgumentNullException(nameof(account));
            }
            
            public override TransactionSignature Initiate(Transaction transaction)
            {
                // Use the account's method to sign the transaction
                return _account.SignTransaction(transaction);
            }
        }
    }
} 