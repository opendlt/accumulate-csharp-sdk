using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 2: Accumulate Identities (ADI) (V3)
/// C# port of Python example_02_accumulate_identities.py
///
/// Demonstrates:
/// - Creating lite identities and token accounts
/// - Creating ADIs (Accumulate Digital Identities) via SmartSigner + TxBody
/// - Adding credits to lite identities and key pages
/// - Querying accounts via the V3 API
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 2: ADI Creation (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        // =========================================================
        // Step 1: Generate key pairs
        // =========================================================
        Console.WriteLine("--- Step 1: Generate Key Pairs ---\n");

        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var adiKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

        var lid = Principal.ComputeUrl(liteKp.GetPublicKey());
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME"));

        Console.WriteLine($"Lite Identity: {lid}");
        Console.WriteLine($"Lite Token Account: {lta}");
        Console.WriteLine($"ADI Key (pubkey): {Convert.ToHexString(adiKp.GetPublicKey()).ToLowerInvariant()[..32]}...\n");

        // =========================================================
        // Step 2: Fund the lite account via faucet
        // =========================================================
        Console.WriteLine("--- Step 2: Fund Account via Faucet ---\n");

        var ltaStr = lta.String();
        Console.WriteLine("Requesting funds from faucet (5 times)...");
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
        // Step 3: Create ADI via SmartSigner + TxBody
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADI ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiName = $"csharp-ex2-{timestamp}";
        var adiUrl = $"acc://{adiName}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";

        Console.WriteLine($"ADI URL: {adiUrl}");
        Console.WriteLine($"Key Book: {keyBookUrl}");
        Console.WriteLine($"Key Page: {keyPageUrl}\n");

        if (balance > 0)
        {
            var liteSigner = new SmartSigner(client.V3, liteKp, lid.String());

            // Get oracle and add credits
            var oracle = await helper.GetOracleAsync();
            Console.WriteLine($"Oracle price: {oracle}");

            var creditAmount = AccumulateHelper.CreditsToAcme(10000, oracle);
            Console.WriteLine($"Adding credits to lite identity ({creditAmount} ACME units)...");
            var creditBody = TxBody.AddCredits(lid.String(), creditAmount.ToString(), oracle);
            var creditResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, creditBody);
            Console.WriteLine($"Credits: {(creditResult.Success ? "OK" : creditResult.Error)}\n");

            await Task.Delay(5000);

            // Create the ADI
            var pubKeyHash = SHA256.HashData(adiKp.GetPublicKey());
            var pubKeyHashHex = Convert.ToHexString(pubKeyHash).ToLowerInvariant();

            Console.WriteLine("Creating ADI...");
            var createBody = TxBody.CreateIdentity(adiUrl, keyBookUrl, pubKeyHashHex);
            var createResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, createBody);

            if (createResult.Success)
            {
                Console.WriteLine($"ADI created successfully! TxId: {createResult.TxId}");

                // Query the new ADI
                await Task.Delay(5000);
                try
                {
                    var adiInfo = await client.V3.QueryAccountAsync(adiUrl);
                    Console.WriteLine($"\nADI info:\n{JsonSerializer.Serialize(adiInfo, new JsonSerializerOptions { WriteIndented = true })}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ADI query: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"ADI creation result: {createResult.Error}");
            }
        }
        else
        {
            Console.WriteLine("Skipping ADI creation (no balance). Demonstrating API shape...");
            Console.WriteLine($"Would call: TxBody.CreateIdentity(\"{adiUrl}\", \"{keyBookUrl}\", \"<keyHash>\")");
        }

        // =========================================================
        // Step 4: Query lite identity info
        // =========================================================
        Console.WriteLine("\n--- Step 4: Query Lite Identity ---\n");

        try
        {
            var lidInfo = await client.V3.QueryAccountAsync(lid.String());
            Console.WriteLine($"Lite Identity query:\n{JsonSerializer.Serialize(lidInfo, new JsonSerializerOptions { WriteIndented = true })}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Query failed: {ex.Message}\n");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("=== Summary ===\n");
        Console.WriteLine($"Lite Identity: {lid}");
        Console.WriteLine($"Lite Token Account: {lta}");
        Console.WriteLine($"Balance: {balance}");
        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine("\nExample 2 COMPLETED SUCCESSFULLY!");
    }
}
