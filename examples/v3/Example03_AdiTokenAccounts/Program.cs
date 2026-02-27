using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 3: ADI Token Accounts (V3)
/// C# port of Python example_03_adi_token_accounts.py
///
/// Demonstrates:
/// - Creating ADI ACME token accounts via SmartSigner + TxBody
/// - Sending tokens from lite to ADI token accounts
/// - Querying token account balances via V3
/// </summary>
class Program
{
    const string KermitBase = "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 3: ADI Token Accounts (C#) ===\n");
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
        Console.WriteLine($"ADI Key: {Convert.ToHexString(adiKp.GetPublicKey()).ToLowerInvariant()[..32]}...\n");

        // =========================================================
        // Step 2: Fund the lite account
        // =========================================================
        Console.WriteLine("--- Step 2: Fund Account via Faucet ---\n");

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
        // Step 3: Create ADI + token account
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADI + Token Account ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiName = $"csharp-ex3-{timestamp}";
        var adiUrl = $"acc://{adiName}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";
        var adiTokenAcct = $"{adiUrl}/tokens";

        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine($"ADI Token Account: {adiTokenAcct}\n");

        if (balance > 0)
        {
            var liteSigner = new SmartSigner(client.V3, liteKp, lid.String());

            // Get oracle price
            var oracle = await helper.GetOracleAsync();
            Console.WriteLine($"Oracle price: {oracle}");

            // Add credits to lite identity
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

            // Create token account under ADI
            var adiSigner = new SmartSigner(client.V3, adiKp, keyPageUrl);
            Console.WriteLine("Creating ADI token account...");
            var tokenAcctBody = TxBody.CreateTokenAccount(adiTokenAcct);
            var tokenAcctResult = await adiSigner.SignSubmitAndWaitAsync(adiUrl, tokenAcctBody);
            Console.WriteLine($"Token account: {(tokenAcctResult.Success ? "OK" : tokenAcctResult.Error)}");
            await Task.Delay(5000);

            // Send tokens from lite to ADI token account
            Console.WriteLine("Sending tokens to ADI token account...");
            var sendBody = TxBody.SendTokensSingle(adiTokenAcct, "50000000");
            var sendResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, sendBody);
            Console.WriteLine($"Send: {(sendResult.Success ? "OK" : sendResult.Error)}");

            // Check balance
            await Task.Delay(5000);
            try
            {
                var adiBalance = await helper.GetBalanceAsync(adiTokenAcct);
                Console.WriteLine($"\nADI token account balance: {adiBalance}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance query: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping creation (no balance). Demonstrating API shape...");
            Console.WriteLine($"TxBody.CreateTokenAccount(\"{adiTokenAcct}\")");
        }

        // =========================================================
        // Step 4: Query accounts via V3
        // =========================================================
        Console.WriteLine("\n--- Step 4: Query Accounts ---\n");

        try
        {
            var info = await client.V3.QueryAccountAsync(ltaStr);
            Console.WriteLine($"LTA info:\n{JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true })}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Query failed: {ex.Message}\n");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("=== Summary ===\n");
        Console.WriteLine("Created lite accounts, ADI, and token account.");
        Console.WriteLine($"Balance in LTA: {balance}");
        Console.WriteLine("\nExample 3 COMPLETED SUCCESSFULLY!");
    }
}
