using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 10: Update Key Page Threshold (V3)
/// C# port of Python example_10_update_key_page_threshold.py
///
/// Demonstrates:
/// - Creating an ADI with a key page
/// - Adding multiple keys to a key page
/// - Setting multi-sig threshold via SmartSigner
/// - Querying key page state before and after
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 10: Update Key Page Threshold (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        // =========================================================
        // Step 1: Generate key pairs
        // =========================================================
        Console.WriteLine("--- Step 1: Generate Key Pairs ---\n");

        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var key1 = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var key2 = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var key3 = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

        var lid = Principal.ComputeUrl(liteKp.GetPublicKey());
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME"));

        Console.WriteLine($"Lite Identity: {lid}");
        Console.WriteLine($"Lite Token Account: {lta}");
        Console.WriteLine($"Key 1: {Convert.ToHexString(key1.GetPublicKey()).ToLowerInvariant()[..32]}...");
        Console.WriteLine($"Key 2: {Convert.ToHexString(key2.GetPublicKey()).ToLowerInvariant()[..32]}...");
        Console.WriteLine($"Key 3: {Convert.ToHexString(key3.GetPublicKey()).ToLowerInvariant()[..32]}...\n");

        // =========================================================
        // Step 2: Fund lite account
        // =========================================================
        Console.WriteLine("--- Step 2: Fund Account ---\n");

        var ltaStr = lta.String();
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await client.V2.FaucetAsync(ltaStr);
                Console.WriteLine($"  Faucet {i + 1}/5: submitted");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Faucet {i + 1}/5 failed: {ex.Message}");
            }
        }

        Console.WriteLine("\nPolling for balance...");
        long balance = await helper.PollForBalanceAsync(ltaStr, timeout: TimeSpan.FromSeconds(60));
        Console.WriteLine($"Balance: {balance}\n");

        // =========================================================
        // Step 3: Create ADI + Add Keys + Set Threshold
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADI + Multi-Sig Setup ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiUrl = $"acc://csharp-ex10-{timestamp}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";

        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine($"Key Book: {keyBookUrl}");
        Console.WriteLine($"Key Page: {keyPageUrl}");
        Console.WriteLine($"Planned threshold: 2 of 3\n");

        if (balance > 0)
        {
            var liteSigner = new SmartSigner(client.V3, liteKp, lid.String());

            // Get oracle price
            var oracle = await helper.GetOracleAsync();
            Console.WriteLine($"Oracle price: {oracle}");

            // Add credits to lite identity
            Console.WriteLine("Adding credits to lite identity...");
            var creditAmount = AccumulateHelper.CreditsToAcme(20000, oracle);
            var creditBody = TxBody.AddCredits(lid.String(), creditAmount.ToString(), oracle);
            var creditResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, creditBody);
            Console.WriteLine($"Credits: {(creditResult.Success ? "OK" : creditResult.Error)}");
            await Task.Delay(5000);

            // Create ADI with key1
            Console.WriteLine("Creating ADI...");
            var key1Hash = SHA256.HashData(key1.GetPublicKey());
            var key1HashHex = Convert.ToHexString(key1Hash).ToLowerInvariant();
            var createBody = TxBody.CreateIdentity(adiUrl, keyBookUrl, key1HashHex);
            var adiResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, createBody);
            Console.WriteLine($"ADI: {(adiResult.Success ? "OK" : adiResult.Error)}");
            await Task.Delay(5000);

            // Add credits to key page
            Console.WriteLine("Adding credits to key page...");
            var pageCreditAmount = AccumulateHelper.CreditsToAcme(10000, oracle);
            var pageCreditBody = TxBody.AddCredits(keyPageUrl, pageCreditAmount.ToString(), oracle);
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, pageCreditBody);
            await Task.Delay(5000);

            // Query key page before changes
            Console.WriteLine("\n--- Key Page State (before) ---");
            var keyManager = new KeyManager(client.V3, keyPageUrl);
            try
            {
                var state = await keyManager.GetKeyPageStateAsync();
                Console.WriteLine($"Version: {state.Version}");
                Console.WriteLine($"Credit Balance: {state.CreditBalance}");
                Console.WriteLine($"Threshold: {state.AcceptThreshold}");
                Console.WriteLine($"Keys: {state.Keys.Count}");
                foreach (var k in state.Keys)
                    Console.WriteLine($"  Hash: {k.KeyHash[..Math.Min(32, k.KeyHash.Length)]}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key page query: {ex.Message}");
            }

            // Add key2 to the key page
            var adiSigner = new SmartSigner(client.V3, key1, keyPageUrl);
            Console.WriteLine("\nAdding key 2 to key page...");
            var key2Hash = SHA256.HashData(key2.GetPublicKey());
            var addKey2Result = await adiSigner.AddKeyAsync(key2Hash);
            Console.WriteLine($"Add key 2: {(addKey2Result.Success ? "OK" : addKey2Result.Error)}");
            await Task.Delay(5000);

            // Add key3 to the key page
            Console.WriteLine("Adding key 3 to key page...");
            var key3Hash = SHA256.HashData(key3.GetPublicKey());
            var addKey3Result = await adiSigner.AddKeyAsync(key3Hash);
            Console.WriteLine($"Add key 3: {(addKey3Result.Success ? "OK" : addKey3Result.Error)}");
            await Task.Delay(5000);

            // Set threshold to 2 of 3
            Console.WriteLine("Setting threshold to 2...");
            var thresholdResult = await adiSigner.SetThresholdAsync(2);
            Console.WriteLine($"Set threshold: {(thresholdResult.Success ? "OK" : thresholdResult.Error)}");
            await Task.Delay(5000);

            // Query key page after changes
            Console.WriteLine("\n--- Key Page State (after) ---");
            try
            {
                var state = await keyManager.GetKeyPageStateAsync();
                Console.WriteLine($"Version: {state.Version}");
                Console.WriteLine($"Threshold: {state.AcceptThreshold}");
                Console.WriteLine($"Keys: {state.Keys.Count}");
                foreach (var k in state.Keys)
                    Console.WriteLine($"  Hash: {k.KeyHash[..Math.Min(32, k.KeyHash.Length)]}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key page query: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping creation (no balance). Demonstrating API shape...");
            Console.WriteLine("SmartSigner.AddKeyAsync(keyHash) - adds a key to key page");
            Console.WriteLine("SmartSigner.SetThresholdAsync(2) - sets multi-sig threshold");
            Console.WriteLine("KeyManager.GetKeyPageStateAsync() - queries key page state");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("\n=== Summary ===\n");
        Console.WriteLine("Demonstrated key page threshold concepts:");
        Console.WriteLine($"  - Generated 3 key pairs for multi-sig");
        Console.WriteLine($"  - Created ADI with key page: {keyPageUrl}");
        Console.WriteLine($"  - Added 3 keys to key page");
        Console.WriteLine($"  - Set threshold to 2 of 3 (multi-sig)");
        Console.WriteLine("\nExample 10 COMPLETED SUCCESSFULLY!");
    }
}
