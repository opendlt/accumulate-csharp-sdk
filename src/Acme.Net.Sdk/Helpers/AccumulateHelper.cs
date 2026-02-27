using System.Text.Json;
using Acme.Net.Sdk.Exceptions;

namespace Acme.Net.Sdk.Helpers
{
    /// <summary>
    /// Mid-level helper providing balance/credit queries, faucet, and polling utilities.
    /// Matches Dart accumulate_helper.dart and Python AccumulateHelper.
    /// </summary>
    public class AccumulateHelper
    {
        private readonly Accumulate _client;

        public AccumulateHelper(Accumulate client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Poll for an account balance to reach a minimum amount.
        /// </summary>
        public async Task<long> PollForBalanceAsync(string url, long minAmount = 0, TimeSpan? timeout = null)
        {
            var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));

            while (DateTimeOffset.UtcNow < deadline)
            {
                try
                {
                    var balance = await GetBalanceAsync(url).ConfigureAwait(false);
                    if (balance > minAmount)
                        return balance;
                }
                catch
                {
                    // Account may not exist yet
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }

            return 0;
        }

        /// <summary>
        /// Poll for credits on an account to reach a minimum amount.
        /// </summary>
        public async Task<long> PollForCreditsAsync(string url, long minCredits = 0, TimeSpan? timeout = null)
        {
            var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));

            while (DateTimeOffset.UtcNow < deadline)
            {
                try
                {
                    var credits = await GetCreditsAsync(url).ConfigureAwait(false);
                    if (credits > minCredits)
                        return credits;
                }
                catch
                {
                    // Account may not exist yet
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }

            return 0;
        }

        /// <summary>
        /// Wait for a transaction to be confirmed.
        /// </summary>
        public async Task<JsonElement> WaitForTxAsync(string txid, TimeSpan? timeout = null)
        {
            var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));

            while (DateTimeOffset.UtcNow < deadline)
            {
                try
                {
                    var result = await _client.V3.QueryTransactionAsync(txid).ConfigureAwait(false);

                    if (result.TryGetProperty("status", out var status))
                    {
                        if (status.TryGetProperty("delivered", out var delivered) &&
                            delivered.ValueKind == JsonValueKind.True)
                        {
                            return result;
                        }
                    }

                    // Check if result itself indicates delivery
                    if (result.TryGetProperty("type", out var type) &&
                        type.GetString() == "transaction")
                    {
                        return result;
                    }
                }
                catch
                {
                    // Transaction may not be indexed yet
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }

            throw new AccumulateTimeoutException(
                $"Transaction {txid} not confirmed within timeout", txid);
        }

        /// <summary>
        /// Get the token balance of an account.
        /// </summary>
        public async Task<long> GetBalanceAsync(string url)
        {
            var result = await _client.V3.QueryAccountAsync(url).ConfigureAwait(false);
            if (result.TryGetProperty("account", out var account) &&
                account.TryGetProperty("balance", out var balProp))
            {
                var balStr = balProp.GetRawText().Trim('"');
                if (long.TryParse(balStr, out var balance))
                    return balance;
            }
            return 0;
        }

        /// <summary>
        /// Get the credit balance of an account.
        /// </summary>
        public async Task<long> GetCreditsAsync(string url)
        {
            var result = await _client.V3.QueryAccountAsync(url).ConfigureAwait(false);
            if (result.TryGetProperty("account", out var account) &&
                account.TryGetProperty("creditBalance", out var credProp))
            {
                var credStr = credProp.GetRawText().Trim('"');
                if (long.TryParse(credStr, out var credits))
                    return credits;
            }
            return 0;
        }

        /// <summary>
        /// Get the oracle price from the network.
        /// Returns the price as an integer (e.g. 10000000 for Kermit, 500000 default).
        /// </summary>
        public async Task<int> GetOracleAsync(int defaultOracle = 500000)
        {
            try
            {
                var networkStatus = await _client.V3.NetworkStatusAsync(new { partition = "directory" }).ConfigureAwait(false);
                if (networkStatus.TryGetProperty("oracle", out var oracleProp) &&
                    oracleProp.TryGetProperty("price", out var priceProp))
                {
                    return priceProp.GetInt32();
                }
            }
            catch { }
            return defaultOracle;
        }

        /// <summary>
        /// Calculate the ACME amount needed to buy the specified number of credits at the given oracle price.
        /// </summary>
        public static long CreditsToAcme(int credits, int oracle)
        {
            return (long)credits * 10_000_000_000L / oracle;
        }

        /// <summary>
        /// Request tokens from the faucet, optionally multiple times with delay.
        /// </summary>
        public async Task<JsonElement> FaucetAsync(string url, int times = 1, TimeSpan? delay = null)
        {
            var d = delay ?? TimeSpan.FromSeconds(2);
            JsonElement lastResult = default;

            for (int i = 0; i < times; i++)
            {
                lastResult = await _client.FaucetAsync(url).ConfigureAwait(false);
                if (i < times - 1)
                    await Task.Delay(d).ConfigureAwait(false);
            }

            return lastResult;
        }
    }
}
