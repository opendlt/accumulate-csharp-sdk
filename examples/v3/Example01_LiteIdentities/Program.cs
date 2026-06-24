using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 1: Lite Identities (V3) - C# port of Python example_01_lite_identities.py
///
/// Demonstrates:
/// - Creating lite identities and token accounts
/// - Funding accounts via faucet
/// - Adding credits via SmartSigner + TxBody
/// - Sending ACME tokens between lite accounts via SmartSigner
/// - Querying account balances via the V3 API
/// </summary>
class Program
{
    // Kermit public testnet
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    // For local DevNet testing, uncomment:
    // const string KermitBase = "http://127.0.0.1:26660";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 1: Lite Identities (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        // =========================================================
        // Step 1: Generate key pairs for two lite identities
        // =========================================================
        Console.WriteLine("--- Step 1: Generate Key Pairs ---\n");

        var kp1 = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var kp2 = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

        // Derive lite identity and token account URLs
        var lid1 = Principal.ComputeUrl(kp1.GetPublicKey());
        var lta1 = Principal.ComputeUrl(kp1.GetPublicKey(), new Url("acc://ACME"));
        var lid2 = Principal.ComputeUrl(kp2.GetPublicKey());
        var lta2 = Principal.ComputeUrl(kp2.GetPublicKey(), new Url("acc://ACME"));

        var lta1Str = lta1.String();
        var lta2Str = lta2.String();

        Console.WriteLine($"Lite Identity 1: {lid1}");
        Console.WriteLine($"Lite Token Account 1: {lta1}");
        Console.WriteLine($"Public Key 1: {Convert.ToHexString(kp1.GetPublicKey()).ToLowerInvariant()[..32]}...\n");

        Console.WriteLine($"Lite Identity 2: {lid2}");
        Console.WriteLine($"Lite Token Account 2: {lta2}");
        Console.WriteLine($"Public Key 2: {Convert.ToHexString(kp2.GetPublicKey()).ToLowerInvariant()[..32]}...\n");

        // =========================================================
        // Step 2: Fund the first lite account via faucet
        // =========================================================
        Console.WriteLine("--- Step 2: Fund Account via Faucet ---\n");

        await FundAccount(client, lta1Str, faucetRequests: 5);

        // Poll for balance
        Console.WriteLine("\nPolling for balance...");
        var balance = await helper.PollForBalanceAsync(lta1Str, timeout: TimeSpan.FromSeconds(60));
        if (balance == 0)
        {
            Console.WriteLine("WARNING: Account not funded. Faucet may not be available.");
            Console.WriteLine("Continuing anyway to demonstrate the API...\n");
        }
        else
        {
            Console.WriteLine($"Balance confirmed: {balance}\n");
        }

        // =========================================================
        // Step 3: Query account info via V3
        // =========================================================
        Console.WriteLine("--- Step 3: Query Account Info (V3) ---\n");

        try
        {
            var accountInfo = await client.V3.QueryAccountAsync(lta1Str);
            Console.WriteLine($"Account query result:\n{JsonSerializer.Serialize(accountInfo, new JsonSerializerOptions { WriteIndented = true })}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Query failed (expected if not funded): {ex.Message}\n");
        }

        // =========================================================
        // Step 4: Add credits to lite identity (required for signing)
        // =========================================================
        Console.WriteLine("--- Step 4: Add Credits (SmartSigner) ---\n");

        if (balance > 0)
        {
            var signer1 = new SmartSigner(client.V3, kp1, lid1.String());

            // Get oracle price from network
            int oracle = 500000; // default
            try
            {
                var networkStatus = await client.V3.NetworkStatusAsync(new { partition = "directory" });
                if (networkStatus.TryGetProperty("oracle", out var oracleProp) &&
                    oracleProp.TryGetProperty("price", out var priceProp))
                {
                    oracle = priceProp.GetInt32();
                }
                Console.WriteLine($"Oracle price: {oracle}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oracle query failed, using default {oracle}: {ex.Message}");
            }

            // Calculate amount for ~5000 credits
            int desiredCredits = 5000;
            long creditAmount = (long)desiredCredits * 10_000_000_000L / oracle;
            Console.WriteLine($"Buying {desiredCredits} credits for {creditAmount} ACME units ({(double)creditAmount / 100_000_000:F2} ACME)");

            var creditBody = TxBody.AddCredits(lid1.String(), creditAmount.ToString(), oracle);
            var creditResult = await signer1.SignSubmitAndWaitAsync(lta1Str, creditBody);
            Console.WriteLine($"AddCredits: {(creditResult.Success ? "OK" : creditResult.Error)}");
            if (creditResult.TxId != null)
                Console.WriteLine($"  TxId: {creditResult.TxId}");
            await Task.Delay(5000);

            // Verify credits
            try
            {
                var credits = await signer1.GetCreditsAsync(refresh: true);
                Console.WriteLine($"Credits after purchase: {credits}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Credits query: {ex.Message}");
            }
        }

        // =========================================================
        // Step 5: Transfer tokens from LTA1 to LTA2 via SmartSigner
        // =========================================================
        Console.WriteLine("\n--- Step 5: Send Tokens (SmartSigner) ---\n");

        if (balance > 0)
        {
            var signer1 = new SmartSigner(client.V3, kp1, lid1.String());
            var sendAmount = "100000000"; // 1 ACME = 100,000,000 units
            var body = TxBody.SendTokensSingle(lta2Str, sendAmount);

            Console.WriteLine($"Sending {sendAmount} units from LTA1 to LTA2...");
            var result = await signer1.SignSubmitAndWaitAsync(lta1Str, body);

            if (result.Success)
            {
                Console.WriteLine($"Transfer successful! TxId: {result.TxId}");
            }
            else
            {
                Console.WriteLine($"Transfer result: {result.Error}");
            }

            // Check balances after transfer
            await Task.Delay(5000);
            try
            {
                var bal1 = await helper.GetBalanceAsync(lta1Str);
                var bal2 = await helper.GetBalanceAsync(lta2Str);
                Console.WriteLine($"\nBalance LTA1 after: {bal1}");
                Console.WriteLine($"Balance LTA2 after: {bal2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance query after transfer: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping transfer (no balance). Demonstrating SmartSigner API shape...");
            var signer1 = new SmartSigner(client.V3, kp1, lid1.String());
            var body = TxBody.SendTokensSingle(lta2Str, "100000000");
            Console.WriteLine($"SmartSigner created for: {signer1.SignerUrl}");
            Console.WriteLine($"Transaction body: {JsonSerializer.Serialize(body)}");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("\n=== Summary ===\n");
        Console.WriteLine("Created two lite identities:");
        Console.WriteLine($"  1. {lid1}");
        Console.WriteLine($"  2. {lid2}");
        Console.WriteLine("Used SmartSigner for token transfer with auto-version tracking.");
        Console.WriteLine("\nExample 1 COMPLETED SUCCESSFULLY!");
    }

    static async Task FundAccount(Accumulate client, string accountUrl, int faucetRequests = 5)
    {
        Console.WriteLine($"Requesting funds from faucet ({faucetRequests} times)...");

        for (int i = 0; i < faucetRequests; i++)
        {
            try
            {
                var result = await client.V2.FaucetAsync(accountUrl);
                var txid = "submitted";
                if (result.TryGetProperty("txid", out var txidProp))
                    txid = txidProp.GetString() ?? "submitted";
                Console.WriteLine($"  Faucet {i + 1}/{faucetRequests}: {txid[..Math.Min(40, txid.Length)]}...");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Faucet {i + 1}/{faucetRequests} failed: {ex.Message}");
            }
        }
    }
}
