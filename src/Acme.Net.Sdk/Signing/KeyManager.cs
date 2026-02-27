using System.Text.Json;
using Acme.Net.Sdk.V3;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Represents a key entry in a key page.
    /// </summary>
    public class KeyEntry
    {
        public string KeyHash { get; set; } = "";
        public string? Delegate { get; set; }
        public long? LastUsedOn { get; set; }
    }

    /// <summary>
    /// Represents the state of a key page.
    /// </summary>
    public class KeyPageState
    {
        public string Url { get; set; } = "";
        public int Version { get; set; } = 1;
        public long CreditBalance { get; set; }
        public int AcceptThreshold { get; set; } = 1;
        public List<KeyEntry> Keys { get; set; } = new();

        public bool HasKey(string keyHash)
        {
            var normalized = keyHash.ToLowerInvariant();
            return Keys.Any(k => k.KeyHash.ToLowerInvariant() == normalized);
        }
    }

    /// <summary>
    /// Queries key page state and creates signers.
    /// Matches Dart key_manager.dart and Python KeyManager.
    /// </summary>
    public class KeyManager
    {
        private readonly AccumulateV3Client _client;
        private readonly string _keyPageUrl;

        public KeyManager(AccumulateV3Client client, string keyPageUrl)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _keyPageUrl = keyPageUrl ?? throw new ArgumentNullException(nameof(keyPageUrl));
        }

        /// <summary>
        /// Queries the current state of the key page.
        /// </summary>
        public async Task<KeyPageState> GetKeyPageStateAsync()
        {
            var result = await _client.QueryAccountAsync(_keyPageUrl).ConfigureAwait(false);
            var state = new KeyPageState { Url = _keyPageUrl };

            if (result.TryGetProperty("account", out var account))
            {
                if (account.TryGetProperty("version", out var version))
                    state.Version = version.GetInt32();

                if (account.TryGetProperty("creditBalance", out var credits))
                {
                    var credStr = credits.GetRawText().Trim('"');
                    if (long.TryParse(credStr, out var creditBalance))
                        state.CreditBalance = creditBalance;
                }

                if (account.TryGetProperty("acceptThreshold", out var threshold))
                    state.AcceptThreshold = threshold.GetInt32();

                if (account.TryGetProperty("keys", out var keys) &&
                    keys.ValueKind == JsonValueKind.Array)
                {
                    foreach (var keyEl in keys.EnumerateArray())
                    {
                        var entry = new KeyEntry();
                        if (keyEl.TryGetProperty("publicKeyHash", out var kh))
                            entry.KeyHash = kh.GetString() ?? "";
                        if (keyEl.TryGetProperty("delegate", out var del))
                            entry.Delegate = del.GetString();
                        if (keyEl.TryGetProperty("lastUsedOn", out var lastUsed))
                        {
                            var luStr = lastUsed.GetRawText().Trim('"');
                            if (long.TryParse(luStr, out var lu))
                                entry.LastUsedOn = lu;
                        }
                        state.Keys.Add(entry);
                    }
                }
            }

            return state;
        }

        /// <summary>
        /// Creates a SmartSigner for the key page using the provided key pair.
        /// </summary>
        public SmartSigner CreateSigner(SignatureKeyPair keypair)
        {
            return new SmartSigner(_client, keypair, _keyPageUrl);
        }
    }
}
