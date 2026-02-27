using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using SignatureKeyPair = Acme.Net.Sdk.Signing.SignatureKeyPair;

namespace Acme.Net.Sdk.Helpers
{
    /// <summary>
    /// A lightweight wallet for quick-start operations.
    /// </summary>
    public class QsWallet
    {
        public SignatureKeyPair Keypair { get; }
        public string LiteIdentityUrl { get; }
        public string LiteTokenAccountUrl { get; }
        public SmartSigner Signer { get; internal set; } = null!;

        public QsWallet(SignatureKeyPair keypair)
        {
            Keypair = keypair;
            var lid = Principal.ComputeUrl(keypair.GetPublicKey());
            var lta = Principal.ComputeUrl(keypair.GetPublicKey(), new Url("acc://ACME"));
            LiteIdentityUrl = lid.String();
            LiteTokenAccountUrl = lta.String();
        }
    }

    /// <summary>
    /// A lightweight ADI for quick-start operations.
    /// </summary>
    public class QsAdi
    {
        public string Name { get; }
        public string Url { get; }
        public string KeyBookUrl { get; }
        public string KeyPageUrl { get; }
        public SignatureKeyPair Keypair { get; }
        public SmartSigner Signer { get; internal set; } = null!;

        public QsAdi(string name, SignatureKeyPair keypair)
        {
            Name = name;
            Url = $"acc://{name}.acme";
            KeyBookUrl = $"acc://{name}.acme/book";
            KeyPageUrl = $"acc://{name}.acme/book/1";
            Keypair = keypair;
        }
    }

    /// <summary>
    /// Ultra-simple API for rapid development and testing.
    /// Matches Dart quick_start.dart and Python QuickStart.
    /// </summary>
    public class QuickStart : IDisposable
    {
        private readonly Accumulate _client;
        private readonly AccumulateHelper _helper;
        private bool _disposed;

        private QuickStart(Accumulate client)
        {
            _client = client;
            _helper = new AccumulateHelper(client);
        }

        /// <summary>
        /// Create a QuickStart connected to a local devnet.
        /// </summary>
        public static QuickStart Devnet(string host = "127.0.0.1", int port = 26660)
        {
            return new QuickStart(Accumulate.Devnet(host, port));
        }

        /// <summary>
        /// Create a QuickStart connected to the public testnet.
        /// </summary>
        public static QuickStart Testnet()
        {
            return new QuickStart(Accumulate.Testnet());
        }

        /// <summary>
        /// Create a QuickStart connected to Kermit testnet.
        /// </summary>
        public static QuickStart Kermit()
        {
            return new QuickStart(new Accumulate("https://kermit.accumulatenetwork.io"));
        }

        // ---- Wallet ----

        /// <summary>
        /// Create a new wallet with a random key pair.
        /// </summary>
        public QsWallet CreateWallet()
        {
            var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var wallet = new QsWallet(kp);
            wallet.Signer = new SmartSigner(_client.V3, kp, wallet.LiteIdentityUrl);
            return wallet;
        }

        /// <summary>
        /// Fund a wallet via the faucet.
        /// </summary>
        public async Task FundWalletAsync(QsWallet wallet, int times = 5, TimeSpan? waitTime = null)
        {
            var wait = waitTime ?? TimeSpan.FromSeconds(2);

            for (int i = 0; i < times; i++)
            {
                try
                {
                    await _client.V2.FaucetAsync(wallet.LiteTokenAccountUrl).ConfigureAwait(false);
                }
                catch
                {
                    // Faucet may fail intermittently
                }
                if (i < times - 1)
                    await Task.Delay(wait).ConfigureAwait(false);
            }

            // Wait for funds to settle
            await _helper.PollForBalanceAsync(wallet.LiteTokenAccountUrl, timeout: TimeSpan.FromSeconds(60))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get the ACME balance of a wallet.
        /// </summary>
        public Task<long> GetBalanceAsync(QsWallet wallet)
        {
            return _helper.GetBalanceAsync(wallet.LiteTokenAccountUrl);
        }

        // ---- ADI Setup ----

        /// <summary>
        /// Set up a full ADI (identity + key book + key page) in a single call.
        /// </summary>
        public async Task<QsAdi> SetupAdiAsync(QsWallet wallet, string adiName)
        {
            var adiKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            var adi = new QsAdi(adiName, adiKp);

            var pubKeyHash = SHA256.HashData(adiKp.GetPublicKey());
            var pubKeyHashHex = Convert.ToHexString(pubKeyHash).ToLowerInvariant();

            var body = TxBody.CreateIdentity(adi.Url, adi.KeyBookUrl, pubKeyHashHex);

            var result = await wallet.Signer.SignSubmitAndWaitAsync(
                wallet.LiteTokenAccountUrl, body).ConfigureAwait(false);

            if (!result.Success)
                throw new Exceptions.AccumulateException($"ADI creation failed: {result.Error}");

            adi.Signer = new SmartSigner(_client.V3, adiKp, adi.KeyPageUrl);
            return adi;
        }

        /// <summary>
        /// Purchase credits for an ADI key page from a lite wallet.
        /// </summary>
        public async Task BuyCreditsForAdiAsync(QsWallet wallet, QsAdi adi, int credits)
        {
            // Estimate ACME needed: credits * 1e4 (approximate at oracle = 5000)
            var amount = ((long)credits * 10000).ToString();
            var body = TxBody.AddCredits(adi.KeyPageUrl, amount, 5000);

            var result = await wallet.Signer.SignSubmitAndWaitAsync(
                wallet.LiteTokenAccountUrl, body).ConfigureAwait(false);

            if (!result.Success)
                throw new Exceptions.AccumulateException($"Credit purchase failed: {result.Error}");
        }

        // ---- Token Operations ----

        /// <summary>
        /// Send ACME tokens from a lite wallet to another account.
        /// </summary>
        public Task<TransactionResult> SendFromLiteAsync(QsWallet wallet, string toAccount, string amount)
        {
            var body = TxBody.SendTokensSingle(toAccount, amount);
            return wallet.Signer.SignSubmitAndWaitAsync(wallet.LiteTokenAccountUrl, body);
        }

        /// <summary>
        /// Create a token account under an ADI.
        /// </summary>
        public Task<TransactionResult> CreateTokenAccountAsync(
            QsAdi adi, string accountName, string tokenUrl = "acc://ACME")
        {
            var accountUrl = $"{adi.Url}/{accountName}";
            var body = TxBody.CreateTokenAccount(accountUrl, tokenUrl);
            return adi.Signer.SignSubmitAndWaitAsync(adi.Url, body);
        }

        // ---- Data Operations ----

        /// <summary>
        /// Create a data account under an ADI.
        /// </summary>
        public Task<TransactionResult> CreateDataAccountAsync(QsAdi adi, string accountName)
        {
            var accountUrl = $"{adi.Url}/{accountName}";
            var body = TxBody.CreateDataAccount(accountUrl);
            return adi.Signer.SignSubmitAndWaitAsync(adi.Url, body);
        }

        /// <summary>
        /// Write data entries to a data account.
        /// </summary>
        public Task<TransactionResult> WriteDataAsync(QsAdi adi, string accountName, List<string> data)
        {
            var accountUrl = $"{adi.Url}/{accountName}";
            // Convert string data to hex
            var hexData = data.Select(d =>
                Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(d)).ToLowerInvariant()
            ).ToList();
            var body = TxBody.WriteData(hexData);
            return adi.Signer.SignSubmitAndWaitAsync(accountUrl, body);
        }

        // ---- Key Management ----

        /// <summary>
        /// Add a key to an ADI's key page.
        /// </summary>
        public Task<TransactionResult> AddKeyToAdiAsync(QsAdi adi, SignatureKeyPair newKey)
        {
            var keyHash = SHA256.HashData(newKey.GetPublicKey());
            return adi.Signer.AddKeyAsync(keyHash);
        }

        /// <summary>
        /// Set the multi-signature threshold on an ADI's key page.
        /// </summary>
        public Task<TransactionResult> SetMultiSigThresholdAsync(QsAdi adi, int threshold)
        {
            return adi.Signer.SetThresholdAsync(threshold);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _disposed = true;
            }
        }
    }
}
