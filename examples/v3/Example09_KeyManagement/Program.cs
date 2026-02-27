using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 9: Key Management (V3)
/// C# port of Python example_09_key_management.py
///
/// Demonstrates:
/// - Generating multiple Ed25519 key pairs
/// - Deriving lite identity URLs from public keys
/// - Key export and import
/// - Adding keys to key pages via SmartSigner
/// - Setting multi-sig thresholds via SmartSigner
/// - Querying key pages via V3 and KeyManager
/// </summary>
class Program
{
    const string KermitBase = "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 9: Key Management (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        // =========================================================
        // Step 1: Generate multiple key pairs
        // =========================================================
        Console.WriteLine("--- Step 1: Generate Key Pairs ---\n");

        var keys = new List<Acme.Net.Sdk.Signing.SignatureKeyPair>();
        for (int i = 0; i < 3; i++)
        {
            var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
            keys.Add(kp);
            var pubHex = Convert.ToHexString(kp.GetPublicKey()).ToLowerInvariant();
            Console.WriteLine($"Key {i + 1}: {pubHex[..32]}...");
        }
        Console.WriteLine();

        // =========================================================
        // Step 2: Derive lite identity URLs
        // =========================================================
        Console.WriteLine("--- Step 2: Derive Lite Identity URLs ---\n");

        foreach (var kp in keys)
        {
            var lid = Principal.ComputeUrl(kp.GetPublicKey());
            Console.WriteLine($"  {lid}");
        }
        Console.WriteLine();

        // =========================================================
        // Step 3: Key export and import
        // =========================================================
        Console.WriteLine("--- Step 3: Key Export & Import ---\n");

        var principal = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
        var exported = principal.ExportToBase64();
        Console.WriteLine($"Exported key (base64): {exported[..32]}...");

        var imported = LiteIdentityPrincipal.ImportFromBase64(exported);
        var origPub = Convert.ToHexString(principal.SignatureKeyPair.GetPublicKey()).ToLowerInvariant();
        var importPub = Convert.ToHexString(imported.SignatureKeyPair.GetPublicKey()).ToLowerInvariant();
        Console.WriteLine($"Original pubkey:  {origPub[..32]}...");
        Console.WriteLine($"Imported pubkey:  {importPub[..32]}...");
        Console.WriteLine($"Keys match: {origPub == importPub}\n");

        // =========================================================
        // Step 4: Fund, create ADI, and manage keys
        // =========================================================
        Console.WriteLine("--- Step 4: Fund & Key Management ---\n");

        var liteKp = keys[0];
        var lid0 = Principal.ComputeUrl(liteKp.GetPublicKey());
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME"));
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

        if (balance > 0)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var adiName = $"csharp-ex9-{timestamp}";
            var adiUrl = $"acc://{adiName}.acme";
            var keyBookUrl = $"{adiUrl}/book";
            var keyPageUrl = $"{keyBookUrl}/1";

            var liteSigner = new SmartSigner(client.V3, liteKp, lid0.String());

            // Get oracle price
            var oracle = await helper.GetOracleAsync();
            Console.WriteLine($"Oracle price: {oracle}");

            // Add credits to lite identity
            Console.WriteLine("Adding credits...");
            var creditAmount = AccumulateHelper.CreditsToAcme(20000, oracle);
            var creditBody = TxBody.AddCredits(lid0.String(), creditAmount.ToString(), oracle);
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, creditBody);
            await Task.Delay(5000);

            // Create ADI with keys[0]
            var adiKp = keys[0];
            var pubKeyHash = SHA256.HashData(adiKp.GetPublicKey());
            var pubKeyHashHex = Convert.ToHexString(pubKeyHash).ToLowerInvariant();

            Console.WriteLine($"Creating ADI: {adiUrl}");
            var createBody = TxBody.CreateIdentity(adiUrl, keyBookUrl, pubKeyHashHex);
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, createBody);
            await Task.Delay(5000);

            // Add credits to ADI key page
            Console.WriteLine("Adding credits to key page...");
            var pageCreditAmount = AccumulateHelper.CreditsToAcme(10000, oracle);
            var pageCreditBody = TxBody.AddCredits(keyPageUrl, pageCreditAmount.ToString(), oracle);
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, pageCreditBody);
            await Task.Delay(5000);

            // Query key page state with KeyManager
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

            // Add keys[1] to the key page
            Console.WriteLine("\nAdding key 2 to key page...");
            var adiSigner = new SmartSigner(client.V3, adiKp, keyPageUrl);
            var newKeyHash = SHA256.HashData(keys[1].GetPublicKey());
            var addResult = await adiSigner.AddKeyAsync(newKeyHash);
            Console.WriteLine($"Add key: {(addResult.Success ? "OK" : addResult.Error)}");
            await Task.Delay(5000);

            // Set threshold to 2 (multi-sig)
            Console.WriteLine("Setting threshold to 2...");
            var thresholdResult = await adiSigner.SetThresholdAsync(2);
            Console.WriteLine($"Set threshold: {(thresholdResult.Success ? "OK" : thresholdResult.Error)}");
            await Task.Delay(5000);

            // Query key page state again
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
            Console.WriteLine("Skipping key management (no balance). Demonstrating API shape...");
            Console.WriteLine("SmartSigner.AddKeyAsync(keyHash) - adds a key to key page");
            Console.WriteLine("SmartSigner.SetThresholdAsync(2) - sets multi-sig threshold");
            Console.WriteLine("KeyManager.GetKeyPageStateAsync() - queries key page state");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("\n=== Summary ===\n");
        Console.WriteLine($"Generated {keys.Count} key pairs");
        Console.WriteLine("Demonstrated key export/import round-trip");
        Console.WriteLine("Demonstrated key page management via SmartSigner");
        Console.WriteLine("Demonstrated key page queries via KeyManager");
        Console.WriteLine("\nExample 9 COMPLETED SUCCESSFULLY!");
    }
}
