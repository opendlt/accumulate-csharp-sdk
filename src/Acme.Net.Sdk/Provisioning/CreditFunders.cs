using System;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Net.Sdk.Exceptions;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.V3;

namespace Acme.Net.Sdk.Provisioning
{
    /// <summary>
    /// Ready-made <see cref="CreditFunderAsync"/> implementations for funding the key pages of
    /// <see cref="CustodyMode.OwnKeyBook"/> levels.
    /// </summary>
    public static class CreditFunders
    {
        /// <summary>
        /// Build a funder that buys credits for a key page by spending ACME from
        /// <paramref name="sourceTokenAccountUrl"/>, signed by <paramref name="sourceSigner"/>.
        /// The source is typically a faucet-funded lite wallet (signer = lite identity,
        /// token account = its lite token account) or an ADI token account.
        /// </summary>
        /// <param name="client">V3 client (used to read the network oracle price).</param>
        /// <param name="sourceSigner">Signer that controls the ACME source and holds credits to pay the <c>addCredits</c> fee.</param>
        /// <param name="sourceTokenAccountUrl">The ACME token account that pays (the transaction principal).</param>
        /// <param name="fixedOracle">Override the oracle price instead of querying the network (optional).</param>
        public static CreditFunderAsync FromTokenAccount(
            AccumulateV3Client client,
            SmartSigner sourceSigner,
            string sourceTokenAccountUrl,
            int? fixedOracle = null)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (sourceSigner == null) throw new ArgumentNullException(nameof(sourceSigner));
            if (string.IsNullOrWhiteSpace(sourceTokenAccountUrl)) throw new ArgumentNullException(nameof(sourceTokenAccountUrl));

            return async (keyPageUrl, credits) =>
            {
                int oracle = fixedOracle ?? await GetOracleAsync(client).ConfigureAwait(false);

                // ACME (in 1e-8 units) needed to buy `credits` credits at `oracle`.
                // Mirrors Acme.Net.Sdk.Helpers.AccumulateHelper.CreditsToAcme.
                long acme = (long)credits * 10_000_000_000L / oracle;

                var body = TxBody.AddCredits(keyPageUrl, acme.ToString(), oracle);
                var res = await sourceSigner.SignSubmitAndWaitAsync(
                    sourceTokenAccountUrl, body, memo: $"fund credits: {keyPageUrl}").ConfigureAwait(false);
                if (res == null || !res.Success)
                    throw new AccumulateException(
                        $"funding {keyPageUrl} with {credits} credits failed: {res?.Error ?? "unknown error"}");
            };
        }

        private static async Task<int> GetOracleAsync(AccumulateV3Client client, int fallback = 500000)
        {
            try
            {
                var ns = await client.NetworkStatusAsync(new { partition = "directory" }).ConfigureAwait(false);
                if (ns.TryGetProperty("oracle", out var o) && o.ValueKind == JsonValueKind.Object &&
                    o.TryGetProperty("price", out var p) && p.ValueKind == JsonValueKind.Number)
                    return p.GetInt32();
            }
            catch
            {
                // fall through to the default
            }
            return fallback;
        }
    }
}
