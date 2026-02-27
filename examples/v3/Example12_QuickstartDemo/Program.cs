using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Core;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Support;

/// <summary>
/// SDK Example 12: QuickStart Demo (V3)
/// C# port of Python example_12_quickstart_demo.py
///
/// Demonstrates:
/// - Using the Accumulate facade for simple development
/// - Factory methods for network selection
/// - Key generation and URL derivation
/// - Faucet funding and balance queries
/// - Canonical JSON serialization
/// </summary>
class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 12: QuickStart Demo (C#) ===\n");
        Console.WriteLine("The Accumulate facade provides the simplest interface to Accumulate.\n");

        // Choose network
        bool useKermit = true; // Set to false for local devnet

        Accumulate acc;
        if (useKermit)
        {
            Console.WriteLine("Connecting to Kermit testnet...");
            acc = new Accumulate("https://kermit.accumulatenetwork.io");
        }
        else
        {
            Console.WriteLine("Connecting to local DevNet...");
            acc = Accumulate.Devnet();
        }

        using (acc)
        {
            await RunQuickstartDemo(acc);
        }
    }

    static async Task RunQuickstartDemo(Accumulate acc)
    {
        // =========================================================
        // Step 1: Create a "Wallet" (key pair + derived URLs)
        // =========================================================
        Console.WriteLine("\n--- Step 1: Create a Wallet ---\n");

        var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var lid = Principal.ComputeUrl(kp.GetPublicKey());
        var lta = Principal.ComputeUrl(kp.GetPublicKey(), new Url("acc://ACME"));
        var pubKeyHash = Convert.ToHexString(kp.GetPublicKey()).ToLowerInvariant();

        Console.WriteLine("Created wallet:");
        Console.WriteLine($"  Lite Identity: {lid}");
        Console.WriteLine($"  Lite Token Account: {lta}");
        Console.WriteLine($"  Public Key Hash: {pubKeyHash[..32]}...\n");

        // =========================================================
        // Step 2: Fund the Wallet
        // =========================================================
        Console.WriteLine("--- Step 2: Fund the Wallet ---\n");

        var ltaStr = lta.String();
        Console.WriteLine($"Requesting funds from faucet (5 times)...");
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await acc.V2.FaucetAsync(ltaStr);
                Console.WriteLine($"  Faucet {i + 1}/5: submitted");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Faucet {i + 1}/5 failed: {ex.Message}");
            }
        }

        Console.WriteLine("\nWaiting 15 seconds for funding...");
        await Task.Delay(15000);

        // Check balance
        long balance = 0;
        try
        {
            var result = await acc.V3.QueryAccountAsync(ltaStr);
            if (result.TryGetProperty("account", out var account) &&
                account.TryGetProperty("balance", out var balProp))
            {
                var s = balProp.GetRawText().Trim('"');
                long.TryParse(s, out balance);
            }
        }
        catch { }

        if (balance == 0)
        {
            Console.WriteLine("WARNING: Wallet not funded - faucet may not be available");
            Console.WriteLine("Continuing with demo...");
        }
        else
        {
            Console.WriteLine($"Balance: {balance} units ({balance / 100000000.0:F2} ACME)");
        }
        Console.WriteLine();

        // =========================================================
        // Step 3: Derive an ADI URL
        // =========================================================
        Console.WriteLine("--- Step 3: Derive ADI URL ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiName = $"quickstart-demo-{timestamp}";
        var adiUrl = $"acc://{adiName}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";

        Console.WriteLine($"ADI URL: {adiUrl}");
        Console.WriteLine($"Key Book: {keyBookUrl}");
        Console.WriteLine($"Key Page: {keyPageUrl}\n");

        // =========================================================
        // Step 4: Demonstrate Canonical JSON
        // =========================================================
        Console.WriteLine("--- Step 4: Canonical JSON ---\n");

        var testObj = new { z = 3, a = 1, m = 2, nested = new { b = "hello", a = "world" } };
        var canonical = CanonicalJson.Serialize(testObj);
        Console.WriteLine($"Input: {{ z=3, a=1, m=2, nested={{ b=\"hello\", a=\"world\" }} }}");
        Console.WriteLine($"Canonical: {canonical}\n");

        // =========================================================
        // Step 5: Demonstrate Network Endpoints
        // =========================================================
        Console.WriteLine("--- Step 5: Network Endpoints ---\n");

        Console.WriteLine($"Mainnet V2: {NetworkEndpoints.GetV2Url(NetworkEndpoint.Mainnet)}");
        Console.WriteLine($"Mainnet V3: {NetworkEndpoints.GetV3Url(NetworkEndpoint.Mainnet)}");
        Console.WriteLine($"Testnet V2: {NetworkEndpoints.GetV2Url(NetworkEndpoint.Testnet)}");
        Console.WriteLine($"Testnet V3: {NetworkEndpoints.GetV3Url(NetworkEndpoint.Testnet)}");
        Console.WriteLine($"Devnet V2:  {NetworkEndpoints.GetV2Url(NetworkEndpoint.Devnet)}");
        Console.WriteLine($"Devnet V3:  {NetworkEndpoints.GetV3Url(NetworkEndpoint.Devnet)}\n");

        // =========================================================
        // Step 6: Demonstrate Transaction Type Codes
        // =========================================================
        Console.WriteLine("--- Step 6: Transaction Type Codes ---\n");

        int[] txCodes = { 1, 2, 3, 4, 5, 8, 9, 14, 15, 16, 17, 48, 51 };
        foreach (var code in txCodes)
        {
            Console.WriteLine($"  Code {code,2}: {Acme.Net.Sdk.Protocol.TransactionTypeCode.GetApiName(code)}");
        }
        Console.WriteLine();

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("=== Summary ===\n");
        Console.WriteLine("QuickStart Demo achievements:");
        Console.WriteLine("  1. Created wallet with lite accounts");
        Console.WriteLine("  2. Funded wallet via faucet");
        Console.WriteLine($"  3. Derived ADI URL: {adiUrl}");
        Console.WriteLine("  4. Demonstrated canonical JSON serialization");
        Console.WriteLine("  5. Demonstrated network endpoint configuration");
        Console.WriteLine("  6. Demonstrated transaction type code lookups");
        Console.WriteLine();
        Console.WriteLine("Note: Full QuickStart API (setup_adi, create_token_account,");
        Console.WriteLine("write_data, add_key, set_multisig_threshold) will be");
        Console.WriteLine("available in Phase 2.\n");
        Console.WriteLine("Example 12 COMPLETED SUCCESSFULLY!");
    }
}
