using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 4: Data Accounts and Entries (V3)
/// C# port of Python example_04_data_accounts_entries.py
///
/// Demonstrates:
/// - Creating ADI Data Accounts via SmartSigner + TxBody
/// - Writing data entries to data accounts
/// - Querying data entries via V3
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 4: Data Accounts and Entries (C#) ===\n");
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
        Console.WriteLine($"Lite Token Account: {lta}\n");

        // =========================================================
        // Step 2: Fund the lite account
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
        // Step 3: Create ADI + Data Account + Write Data
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADI + Data Account ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiName = $"csharp-ex4-{timestamp}";
        var adiUrl = $"acc://{adiName}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";
        var dataAccountUrl = $"{adiUrl}/data";

        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine($"Data Account: {dataAccountUrl}\n");

        if (balance > 0)
        {
            var liteSigner = new SmartSigner(client.V3, liteKp, lid.String());

            // Get oracle price
            var oracle = await helper.GetOracleAsync();
            Console.WriteLine($"Oracle price: {oracle}");

            // Add credits
            Console.WriteLine("Adding credits to lite identity...");
            var creditAmount = AccumulateHelper.CreditsToAcme(10000, oracle);
            var creditBody = TxBody.AddCredits(lid.String(), creditAmount.ToString(), oracle);
            var creditResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, creditBody);
            Console.WriteLine($"Credits: {(creditResult.Success ? "OK" : creditResult.Error)}");
            await Task.Delay(5000);

            // Create ADI
            Console.WriteLine("Creating ADI...");
            var pubKeyHash = SHA256.HashData(adiKp.GetPublicKey());
            var pubKeyHashHex = Convert.ToHexString(pubKeyHash).ToLowerInvariant();
            var createAdiBody = TxBody.CreateIdentity(adiUrl, keyBookUrl, pubKeyHashHex);
            var adiResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, createAdiBody);
            Console.WriteLine($"ADI: {(adiResult.Success ? "OK" : adiResult.Error)}");
            await Task.Delay(5000);

            // Add credits to ADI key page
            Console.WriteLine("Adding credits to ADI key page...");
            var adiCreditAmount = AccumulateHelper.CreditsToAcme(5000, oracle);
            var adiCreditBody = TxBody.AddCredits(keyPageUrl, adiCreditAmount.ToString(), oracle);
            var adiCreditResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, adiCreditBody);
            Console.WriteLine($"ADI credits: {(adiCreditResult.Success ? "OK" : adiCreditResult.Error)}");
            await Task.Delay(5000);

            // Create data account
            var adiSigner = new SmartSigner(client.V3, adiKp, keyPageUrl);
            Console.WriteLine("Creating data account...");
            var dataAcctBody = TxBody.CreateDataAccount(dataAccountUrl);
            var dataAcctResult = await adiSigner.SignSubmitAndWaitAsync(adiUrl, dataAcctBody);
            Console.WriteLine($"Data account: {(dataAcctResult.Success ? "OK" : dataAcctResult.Error)}");
            await Task.Delay(5000);

            // Write data entry
            Console.WriteLine("Writing data entry...");
            var dataHex = new List<string>
            {
                Convert.ToHexString(Encoding.UTF8.GetBytes("Hello from C# SDK!")).ToLowerInvariant(),
                Convert.ToHexString(Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o"))).ToLowerInvariant(),
            };
            var writeBody = TxBody.WriteData(dataHex);
            var writeResult = await adiSigner.SignSubmitAndWaitAsync(dataAccountUrl, writeBody);
            Console.WriteLine($"Write data: {(writeResult.Success ? "OK" : writeResult.Error)}");

            // Query data
            await Task.Delay(5000);
            try
            {
                var dataResult = await client.V3.QueryDataAsync(dataAccountUrl);
                Console.WriteLine($"\nData query:\n{JsonSerializer.Serialize(dataResult, new JsonSerializerOptions { WriteIndented = true })}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data query: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping creation (no balance). Demonstrating API shape...");
            Console.WriteLine($"TxBody.CreateDataAccount(\"{dataAccountUrl}\")");
            var sampleData = new List<string> { Convert.ToHexString(Encoding.UTF8.GetBytes("Hello")).ToLowerInvariant() };
            Console.WriteLine($"TxBody.WriteData({JsonSerializer.Serialize(sampleData)})");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("\n=== Summary ===\n");
        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine($"Data Account: {dataAccountUrl}");
        Console.WriteLine("\nExample 4 COMPLETED SUCCESSFULLY!");
    }
}
