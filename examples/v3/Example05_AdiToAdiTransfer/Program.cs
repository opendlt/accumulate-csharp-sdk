using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 5: ADI-to-ADI Token Transfer (V3)
/// C# port of Python example_05_adi_to_adi_transfer.py
///
/// Demonstrates:
/// - Creating two ADIs with token accounts
/// - Sending ACME tokens between ADI token accounts via SmartSigner
/// - Querying balances before and after transfers via V3
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 5: ADI-to-ADI Token Transfer (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        // =========================================================
        // Step 1: Generate key pairs
        // =========================================================
        Console.WriteLine("--- Step 1: Generate Key Pairs ---\n");

        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var senderKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var receiverKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);

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
        // Step 3: Create sender and receiver ADIs + token accounts
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADIs + Token Accounts ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var senderAdiName = $"csharp-sender-{timestamp}";
        var senderAdi = $"acc://{senderAdiName}.acme";
        var senderBook = $"{senderAdi}/book";
        var senderPage = $"{senderBook}/1";
        var senderTokens = $"{senderAdi}/tokens";

        var receiverAdiName = $"csharp-recv-{timestamp}";
        var receiverAdi = $"acc://{receiverAdiName}.acme";
        var receiverBook = $"{receiverAdi}/book";
        var receiverPage = $"{receiverBook}/1";
        var receiverTokens = $"{receiverAdi}/tokens";

        Console.WriteLine($"Sender ADI: {senderAdi}");
        Console.WriteLine($"Sender Token Account: {senderTokens}");
        Console.WriteLine($"Receiver ADI: {receiverAdi}");
        Console.WriteLine($"Receiver Token Account: {receiverTokens}\n");

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

            // Create sender ADI
            Console.WriteLine("Creating sender ADI...");
            var senderHash = Convert.ToHexString(SHA256.HashData(senderKp.GetPublicKey())).ToLowerInvariant();
            var createSender = TxBody.CreateIdentity(senderAdi, senderBook, senderHash);
            var senderResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, createSender);
            Console.WriteLine($"Sender ADI: {(senderResult.Success ? "OK" : senderResult.Error)}");
            await Task.Delay(5000);

            // Create receiver ADI
            Console.WriteLine("Creating receiver ADI...");
            var receiverHash = Convert.ToHexString(SHA256.HashData(receiverKp.GetPublicKey())).ToLowerInvariant();
            var createReceiver = TxBody.CreateIdentity(receiverAdi, receiverBook, receiverHash);
            var receiverResult = await liteSigner.SignSubmitAndWaitAsync(ltaStr, createReceiver);
            Console.WriteLine($"Receiver ADI: {(receiverResult.Success ? "OK" : receiverResult.Error)}");
            await Task.Delay(5000);

            // Add credits to both key pages
            Console.WriteLine("Adding credits to sender key page...");
            var pageCredits = AccumulateHelper.CreditsToAcme(5000, oracle);
            var senderCredits = TxBody.AddCredits(senderPage, pageCredits.ToString(), oracle);
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, senderCredits);
            await Task.Delay(3000);

            Console.WriteLine("Adding credits to receiver key page...");
            var receiverCredits = TxBody.AddCredits(receiverPage, pageCredits.ToString(), oracle);
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, receiverCredits);
            await Task.Delay(5000);

            // Create token accounts
            var senderSigner = new SmartSigner(client.V3, senderKp, senderPage);
            Console.WriteLine("Creating sender token account...");
            var senderAcct = TxBody.CreateTokenAccount(senderTokens);
            await senderSigner.SignSubmitAndWaitAsync(senderAdi, senderAcct);
            await Task.Delay(3000);

            var receiverSigner = new SmartSigner(client.V3, receiverKp, receiverPage);
            Console.WriteLine("Creating receiver token account...");
            var receiverAcct = TxBody.CreateTokenAccount(receiverTokens);
            await receiverSigner.SignSubmitAndWaitAsync(receiverAdi, receiverAcct);
            await Task.Delay(5000);

            // Fund sender token account from lite
            Console.WriteLine("Funding sender token account...");
            var fundBody = TxBody.SendTokensSingle(senderTokens, "200000000");
            await liteSigner.SignSubmitAndWaitAsync(ltaStr, fundBody);
            await Task.Delay(5000);

            // ADI-to-ADI transfer
            Console.WriteLine("\n--- ADI-to-ADI Transfer ---\n");
            var transferBody = TxBody.SendTokensSingle(receiverTokens, "50000000");
            var transferResult = await senderSigner.SignSubmitAndWaitAsync(senderTokens, transferBody);
            Console.WriteLine($"Transfer: {(transferResult.Success ? "OK" : transferResult.Error)}");

            // Check balances
            await Task.Delay(5000);
            try
            {
                var senderBal = await helper.GetBalanceAsync(senderTokens);
                var receiverBal = await helper.GetBalanceAsync(receiverTokens);
                Console.WriteLine($"Sender balance: {senderBal}");
                Console.WriteLine($"Receiver balance: {receiverBal}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance query: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping creation (no balance). Demonstrating API shape...");
            Console.WriteLine($"TxBody.SendTokensSingle(\"{receiverTokens}\", \"50000000\")");
        }

        // =========================================================
        // Step 4: Query network status via V3
        // =========================================================
        Console.WriteLine("\n--- Step 4: Query Network Status ---\n");

        try
        {
            var nodeInfo = await client.V3.NodeInfoAsync();
            Console.WriteLine($"Node info:\n{JsonSerializer.Serialize(nodeInfo, new JsonSerializerOptions { WriteIndented = true })}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Node info query failed: {ex.Message}\n");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("=== Summary ===\n");
        Console.WriteLine($"Funded lite account: {lta}");
        Console.WriteLine($"Sender ADI: {senderAdi}");
        Console.WriteLine($"Receiver ADI: {receiverAdi}");
        Console.WriteLine("\nExample 5 COMPLETED SUCCESSFULLY!");
    }
}
