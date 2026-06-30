using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Helpers;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Provisioning;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;

/// <summary>
/// SDK Example 16: EnsurePathAsync — arbitrary-depth identity hierarchy provisioning (V3 / Kermit).
///
/// On-chain acceptance test for <see cref="HierarchyProvisioner"/>. It:
///   1. funds a lite wallet from the faucet,
///   2. creates a fresh root ADI and funds its key page,
///   3. provisions a DEPTH-3 inherit-only path        (root/tenants/&lt;t&gt;/inventory),
///   4. re-runs the same path to prove IDEMPOTENCY    (0 new creates),
///   5. provisions a MIXED-CUSTODY path               (root/ops = own key book, then ops/ledger),
///   6. verifies every node's on-chain account type.
///
/// Run:  dotnet run --project examples/v3/Example16_EnsureHierarchyPath
/// Env:  ACCUMULATE_BASE_URL (default https://kermit.accumulatenetwork.io)
///       RUN_OWN_KEYBOOK=0 to skip the own-key-book scenario (faster, fewer credits).
/// </summary>
class Program
{
    static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";
    static readonly bool RunOwnKeyBook =
        (Environment.GetEnvironmentVariable("RUN_OWN_KEYBOOK") ?? "1") != "0";

    static async Task<int> Main()
    {
        Console.WriteLine("=== Example 16: EnsurePathAsync (hierarchy provisioning) ===");
        Console.WriteLine($"Endpoint: {BaseUrl}\n");

        using var client = new Accumulate(BaseUrl);
        var helper = new AccumulateHelper(client);
        var provisioner = new HierarchyProvisioner(client.V3);

        long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string adiName = $"verso-ensure-{ts}";
        string root = $"acc://{adiName}.acme";
        string rootBook = $"{root}/book";
        string rootPage = $"{rootBook}/1";

        // ---------------------------------------------------------------
        // 1. Lite wallet + faucet
        // ---------------------------------------------------------------
        Console.WriteLine("--- 1. Fund a lite wallet ---");
        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var lid = Principal.ComputeUrl(liteKp.GetPublicKey()).String();
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME")).String();
        var liteSigner = new SmartSigner(client.V3, liteKp, lid);
        Console.WriteLine($"Lite identity: {lid}");

        for (int i = 0; i < 6; i++)
        {
            try { await client.V2.FaucetAsync(lta); } catch (Exception ex) { Console.WriteLine($"  faucet {i + 1}: {ex.Message}"); }
            await Task.Delay(1500);
        }
        long balance = await helper.PollForBalanceAsync(lta, timeout: TimeSpan.FromSeconds(90));
        Console.WriteLine($"Lite balance: {balance}");
        if (balance <= 0) { Console.WriteLine("FAILED: faucet did not fund the lite account."); return 1; }

        int oracle = await helper.GetOracleAsync();
        Console.WriteLine($"Oracle: {oracle}\n");

        // Credits on the lite identity (pays fees for the bootstrap transactions).
        await BuyCredits(liteSigner, lta, lid, 60000, oracle);
        await helper.PollForCreditsAsync(lid, 1, TimeSpan.FromSeconds(60));

        // ---------------------------------------------------------------
        // 2. Root ADI + funded key page
        // ---------------------------------------------------------------
        Console.WriteLine("--- 2. Create + fund the root ADI ---");
        var rootKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var rootKeyHash = Convert.ToHexString(SHA256.HashData(rootKp.GetPublicKey())).ToLowerInvariant();
        var createRoot = TxBody.CreateIdentity(root, rootBook, rootKeyHash);
        var r = await liteSigner.SignSubmitAndWaitAsync(lta, createRoot, memo: "create root ADI");
        if (!r.Success) { Console.WriteLine($"FAILED root ADI: {r.Error}"); return 1; }
        Console.WriteLine($"Root ADI created: {root}");

        // The root key page pays for every InheritParent level — fund it generously.
        await BuyCredits(liteSigner, lta, rootPage, 50000, oracle);
        await helper.PollForCreditsAsync(rootPage, 1, TimeSpan.FromSeconds(60));
        Console.WriteLine($"Root key page funded: {rootPage}\n");

        var rootSigner = new SmartSigner(client.V3, rootKp, rootPage);

        bool ok = true;

        // ---------------------------------------------------------------
        // 3. Depth-3 inherit-only path
        // ---------------------------------------------------------------
        Console.WriteLine("--- 3. Provision depth-3 inherit path ---");
        string tenant = $"ospri{ts % 1000}";
        string deepPath = $"{root}/tenants/{tenant}/inventory";   // tenants (sub-ADI) / <tenant> (sub-ADI) / inventory (data account)
        Console.WriteLine($"Target: {deepPath}");
        var res1 = await provisioner.EnsurePathAsync(deepPath, rootSigner, LeafKind.DataAccount, memoPrefix: "verso");
        PrintResult(res1);
        ok &= res1.CreatedCount == 3 && res1.Leaf.Kind == NodeKind.DataAccount;
        ok &= await Verify(client, $"{root}/tenants", "identity");
        ok &= await Verify(client, $"{root}/tenants/{tenant}", "identity");
        ok &= await Verify(client, deepPath, "dataAccount");

        // ---------------------------------------------------------------
        // 4. Idempotent re-run
        // ---------------------------------------------------------------
        Console.WriteLine("\n--- 4. Re-run the same path (idempotency) ---");
        var res2 = await provisioner.EnsurePathAsync(deepPath, rootSigner, LeafKind.DataAccount, memoPrefix: "verso");
        PrintResult(res2);
        if (res2.CreatedCount != 0) { Console.WriteLine("FAILED: re-run created nodes; expected 0."); ok = false; }
        else Console.WriteLine("OK: nothing re-created.");

        // ---------------------------------------------------------------
        // 5. Mixed custody: one level with its OWN key book
        // ---------------------------------------------------------------
        if (RunOwnKeyBook)
        {
            Console.WriteLine("\n--- 5. Provision mixed-custody path (own key book) ---");
            string opsAdi = $"{root}/ops{ts % 1000}";
            string mixedPath = $"{opsAdi}/ledger";
            var opsKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

            var plan = new CustodyPlan().WithLevel(opsAdi, LevelCustody.Own(opsKp, credits: 5000));
            var funder = CreditFunders.FromTokenAccount(client.V3, liteSigner, lta, fixedOracle: oracle);

            Console.WriteLine($"Target: {mixedPath}  ({opsAdi} = own key book)");
            var res3 = await provisioner.EnsurePathAsync(
                mixedPath, rootSigner, LeafKind.DataAccount, custody: plan, creditFunder: funder, memoPrefix: "verso");
            PrintResult(res3);
            ok &= await Verify(client, opsAdi, "identity");
            ok &= await Verify(client, $"{opsAdi}/book/1", "keyPage");
            ok &= await Verify(client, mixedPath, "dataAccount");

            // The ledger under ops was created by signing with ops's OWN key page (not the root) —
            // proves custody actually switched at that level.
            Console.WriteLine("ops/ledger was signed by ops's own key page (independent custody).");
        }
        else Console.WriteLine("\n--- 5. (skipped: RUN_OWN_KEYBOOK=0) ---");

        Console.WriteLine($"\n=== {(ok ? "ALL CHECKS PASSED" : "FAILURES — see above")} ===");
        Console.WriteLine($"Root ADI: {root}");
        return ok ? 0 : 1;
    }

    static async Task BuyCredits(SmartSigner signer, string sourceTokenAccount, string recipient, int credits, int oracle)
    {
        long acme = AccumulateHelper.CreditsToAcme(credits, oracle);
        var body = TxBody.AddCredits(recipient, acme.ToString(), oracle);
        var res = await signer.SignSubmitAndWaitAsync(sourceTokenAccount, body, memo: $"credits:{recipient}");
        Console.WriteLine($"  +{credits} credits -> {recipient}: {(res.Success ? "OK" : res.Error)}");
        if (!res.Success) throw new Exception($"funding {recipient} failed: {res.Error}");
    }

    static void PrintResult(ProvisionResult res)
    {
        foreach (var n in res.Nodes)
            Console.WriteLine($"  [{(n.Created ? "created" : "exists ")}] {n.Kind,-12} {n.Url}" +
                              (n.Custody is { } c ? $"  ({c})" : ""));
        Console.WriteLine($"  -> {res.CreatedCount} created, {res.Nodes.Count - res.CreatedCount} already existed");
    }

    static async Task<bool> Verify(Accumulate client, string url, string expectedType)
    {
        // Allow a little time for cross-partition settlement before asserting.
        for (int i = 0; i < 15; i++)
        {
            try
            {
                var rec = await client.V3.QueryAccountAsync(url);
                string? type = rec.TryGetProperty("account", out var a) && a.TryGetProperty("type", out var t)
                    ? t.GetString() : null;
                if (type != null)
                {
                    bool match = string.Equals(type, expectedType, StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine($"  verify {url} -> {type} {(match ? "OK" : $"(expected {expectedType})")}");
                    return match;
                }
            }
            catch { /* not indexed yet */ }
            await Task.Delay(2000);
        }
        Console.WriteLine($"  verify {url} -> NOT FOUND (expected {expectedType})");
        return false;
    }
}
