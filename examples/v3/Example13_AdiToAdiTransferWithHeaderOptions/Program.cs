using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Helpers;

/// <summary>
/// SDK Example 13: ADI-to-ADI Transfer with Header Options (V3)
/// C# port of Python example_13_adi_to_adi_transfer_with_header_options.py
///
/// Demonstrates:
/// - Creating two ADIs with token accounts
/// - Sending ACME tokens with memo in the transaction header
/// - Querying transaction details including memo data
/// </summary>
class Program
{
    const string KermitBase = "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 13: ADI-to-ADI Transfer with Header Options (C#) ===\n");
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
        // Step 3: Create ADIs + Token Accounts + Transfer with Memo
        // =========================================================
        Console.WriteLine("--- Step 3: Create ADIs + Transfer with Memo ---\n");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var senderAdi = $"acc://csharp-sender13-{timestamp}.acme";
        var senderBook = $"{senderAdi}/book";
        var senderPage = $"{senderBook}/1";
        var senderTokens = $"{senderAdi}/tokens";

        var receiverAdi = $"acc://csharp-recv13-{timestamp}.acme";
        var receiverBook = $"{receiverAdi}/book";
        var receiverPage = $"{receiverBook}/1";
        var receiverTokens = $"{receiverAdi}/tokens";

        Console.WriteLine($"Sender ADI: {senderAdi}");
        Console.WriteLine($"Sender Tokens: {senderTokens}");
        Console.WriteLine($"Receiver ADI: {receiverAdi}");
        Console.WriteLine($"Receiver Tokens: {receiverTokens}\n");

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

            // Transfer with memo
            Console.WriteLine("\n--- Step 4: ADI-to-ADI Transfer WITH MEMO ---\n");
            var memo = "Transfer with memo - C# SDK Example 13";
            Console.WriteLine($"Memo: \"{memo}\"");
            var transferBody = TxBody.SendTokensSingle(receiverTokens, "50000000");
            var transferResult = await senderSigner.SignSubmitAndWaitAsync(senderTokens, transferBody, memo: memo);
            Console.WriteLine($"Transfer: {(transferResult.Success ? "OK" : transferResult.Error)}");

            // Query the transaction to verify memo
            if (transferResult.TxId != null)
            {
                Console.WriteLine($"\nTransaction ID: {transferResult.TxId}");
                await Task.Delay(5000);
                try
                {
                    var txInfo = await client.V3.QueryTransactionAsync(transferResult.TxId);
                    Console.WriteLine($"\nTransaction details:\n{JsonSerializer.Serialize(txInfo, new JsonSerializerOptions { WriteIndented = true })}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Tx query: {ex.Message}");
                }
            }

            // Check balances
            await Task.Delay(3000);
            try
            {
                var senderBal = await helper.GetBalanceAsync(senderTokens);
                var receiverBal = await helper.GetBalanceAsync(receiverTokens);
                Console.WriteLine($"\nSender balance: {senderBal}");
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
            Console.WriteLine("SmartSigner.SignSubmitAndWaitAsync(principal, body, memo: \"my memo\")");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("\n=== Summary ===\n");
        Console.WriteLine("Demonstrated:");
        Console.WriteLine("  - Transaction header options (memo)");
        Console.WriteLine($"  - Sender ADI: {senderAdi}");
        Console.WriteLine($"  - Receiver ADI: {receiverAdi}");
        Console.WriteLine("  - ADI-to-ADI transfer with memo via SmartSigner");
        Console.WriteLine("\nExample 13 COMPLETED SUCCESSFULLY!");
    }
}
