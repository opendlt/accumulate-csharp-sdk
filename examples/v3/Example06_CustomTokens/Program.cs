using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 6: Custom Tokens (V3)
/// C# port of Python example_06_custom_tokens.py
///
/// Demonstrates:
/// - Creating custom token issuers
/// - Creating token accounts for custom tokens
/// - Issuing tokens to accounts
/// - Transferring custom tokens between accounts
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 6: Custom Tokens (C#) ===\n");
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
        // Step 3: Create ADI + Custom Token
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADI + Custom Token ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiName = $"csharp-ex6-{timestamp}";
        var adiUrl = $"acc://{adiName}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";
        var tokenIssuerUrl = $"{adiUrl}/my-token";
        var tokenAcct1 = $"{adiUrl}/token-acct-1";
        var tokenAcct2 = $"{adiUrl}/token-acct-2";

        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine($"Custom Token Issuer: {tokenIssuerUrl}");
        Console.WriteLine($"Token Account 1: {tokenAcct1}");
        Console.WriteLine($"Token Account 2: {tokenAcct2}\n");

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
            var adiCreditAmount = AccumulateHelper.CreditsToAcme(10000, oracle);
            var adiCreditBody = TxBody.AddCredits(keyPageUrl, adiCreditAmount.ToString(), oracle);
            var adiCreditResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, adiCreditBody);
            Console.WriteLine($"ADI credits: {(adiCreditResult.Success ? "OK" : adiCreditResult.Error)}");
            await Task.Delay(5000);

            // Create custom token issuer
            var adiSigner = new SmartSigner(client.V3, adiKp, keyPageUrl);
            Console.WriteLine("Creating custom token issuer...");
            var createTokenBody = TxBody.CreateToken(tokenIssuerUrl, "MYT", 8, supplyLimit: "100000000000000");
            var tokenResult = await adiSigner.SignSubmitAndWaitAsync(adiUrl, createTokenBody);
            Console.WriteLine($"Token issuer: {(tokenResult.Success ? "OK" : tokenResult.Error)}");
            await Task.Delay(5000);

            // Create token accounts for the custom token
            Console.WriteLine("Creating token account 1...");
            var acct1Body = TxBody.CreateTokenAccount(tokenAcct1, tokenIssuerUrl);
            var acct1Result = await adiSigner.SignSubmitAndWaitAsync(adiUrl, acct1Body);
            Console.WriteLine($"Token account 1: {(acct1Result.Success ? "OK" : acct1Result.Error)}");
            await Task.Delay(3000);

            Console.WriteLine("Creating token account 2...");
            var acct2Body = TxBody.CreateTokenAccount(tokenAcct2, tokenIssuerUrl);
            var acct2Result = await adiSigner.SignSubmitAndWaitAsync(adiUrl, acct2Body);
            Console.WriteLine($"Token account 2: {(acct2Result.Success ? "OK" : acct2Result.Error)}");
            await Task.Delay(5000);

            // Issue tokens to account 1
            Console.WriteLine("\n--- Step 4: Issue + Transfer Custom Tokens ---\n");
            Console.WriteLine("Issuing tokens to account 1...");
            var issueBody = TxBody.IssueTokens(tokenAcct1, "50000000000");
            var issueResult = await adiSigner.SignSubmitAndWaitAsync(tokenIssuerUrl, issueBody);
            Console.WriteLine($"Issue tokens: {(issueResult.Success ? "OK" : issueResult.Error)}");
            await Task.Delay(5000);

            // Transfer custom tokens from account 1 to account 2
            Console.WriteLine("Transferring custom tokens from acct 1 to acct 2...");
            var transferBody = TxBody.SendTokensSingle(tokenAcct2, "10000000000");
            var transferResult = await adiSigner.SignSubmitAndWaitAsync(tokenAcct1, transferBody);
            Console.WriteLine($"Transfer: {(transferResult.Success ? "OK" : transferResult.Error)}");
            await Task.Delay(5000);

            // Query balances
            Console.WriteLine("\n--- Step 5: Query Balances ---\n");
            try
            {
                var bal1 = await helper.GetBalanceAsync(tokenAcct1);
                var bal2 = await helper.GetBalanceAsync(tokenAcct2);
                Console.WriteLine($"Token Account 1 balance: {bal1}");
                Console.WriteLine($"Token Account 2 balance: {bal2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance query: {ex.Message}");
            }

            // Query the token issuer
            try
            {
                var issuerInfo = await client.V3.QueryAccountAsync(tokenIssuerUrl);
                Console.WriteLine($"\nToken issuer info:\n{JsonSerializer.Serialize(issuerInfo, new JsonSerializerOptions { WriteIndented = true })}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token issuer query: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping creation (no balance). Demonstrating API shape...");
            Console.WriteLine($"TxBody.CreateToken(\"{tokenIssuerUrl}\", \"MYT\", 8, supplyLimit: \"100000000000000\")");
            Console.WriteLine($"TxBody.IssueTokens(\"{tokenAcct1}\", \"50000000000\")");
            Console.WriteLine($"TxBody.SendTokensSingle(\"{tokenAcct2}\", \"10000000000\")");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("\n=== Summary ===\n");
        Console.WriteLine($"ADI: {adiUrl}");
        Console.WriteLine($"Custom Token Issuer: {tokenIssuerUrl}");
        Console.WriteLine($"Token Account 1: {tokenAcct1}");
        Console.WriteLine($"Token Account 2: {tokenAcct2}");
        Console.WriteLine("\nExample 6 COMPLETED SUCCESSFULLY!");
    }
}
