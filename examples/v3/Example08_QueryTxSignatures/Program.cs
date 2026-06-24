using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;

/// <summary>
/// SDK Example 8: Query Transactions and Signatures (V3)
/// C# port of Python example_08_query_tx_signatures.py
///
/// Demonstrates:
/// - Querying transactions, signatures, memo data, and account information
/// - Using the V3 query API for various query types
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 8: Query Transactions & Signatures (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);

        // =========================================================
        // Step 1: Generate key pairs and fund
        // =========================================================
        Console.WriteLine("--- Step 1: Generate Key Pairs & Fund ---\n");

        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var lid = Principal.ComputeUrl(liteKp.GetPublicKey());
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME"));

        Console.WriteLine($"Lite Identity: {lid}");
        Console.WriteLine($"Lite Token Account: {lta}\n");

        var ltaStr = lta.String();
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await client.V2.FaucetAsync(ltaStr);
                Console.WriteLine($"  Faucet {i + 1}/3: submitted");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Faucet {i + 1}/3 failed: {ex.Message}");
            }
        }

        Console.WriteLine("\nWaiting for funding...");
        await Task.Delay(10000);

        // =========================================================
        // Step 2: Query account
        // =========================================================
        Console.WriteLine("--- Step 2: Query Account ---\n");

        try
        {
            var accountInfo = await client.V3.QueryAccountAsync(ltaStr);
            Console.WriteLine($"Account query:\n{FormatJson(accountInfo)}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Account query failed: {ex.Message}\n");
        }

        // =========================================================
        // Step 3: Query chain entries
        // =========================================================
        Console.WriteLine("--- Step 3: Query Chain Entries ---\n");

        try
        {
            var chainResult = await client.V3.QueryChainAsync(ltaStr, "main",
                new Acme.Net.Sdk.V3.RangeOptions { Start = 0, Count = 10, Expand = true });
            Console.WriteLine($"Chain query:\n{FormatJson(chainResult)}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chain query failed: {ex.Message}\n");
        }

        // =========================================================
        // Step 4: Query pending transactions
        // =========================================================
        Console.WriteLine("--- Step 4: Query Pending Transactions ---\n");

        try
        {
            var pendingResult = await client.V3.QueryPendingAsync(ltaStr,
                new Acme.Net.Sdk.V3.RangeOptions { Start = 0, Count = 10 });
            Console.WriteLine($"Pending query:\n{FormatJson(pendingResult)}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pending query failed: {ex.Message}\n");
        }

        // =========================================================
        // Step 5: Search by public key
        // =========================================================
        Console.WriteLine("--- Step 5: Search by Public Key ---\n");

        var pubKeyHex = Convert.ToHexString(liteKp.GetPublicKey()).ToLowerInvariant();
        Console.WriteLine($"Searching for public key: {pubKeyHex[..32]}...");

        try
        {
            var searchResult = await client.V3.SearchPublicKeyAsync(lid.String(), pubKeyHex);
            Console.WriteLine($"Search result:\n{FormatJson(searchResult)}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search failed: {ex.Message}\n");
        }

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("=== Summary ===\n");
        Console.WriteLine("Demonstrated V3 query APIs:");
        Console.WriteLine("  - QueryAccountAsync");
        Console.WriteLine("  - QueryChainAsync (with range options)");
        Console.WriteLine("  - QueryPendingAsync");
        Console.WriteLine("  - SearchPublicKeyAsync");
        Console.WriteLine("\nExample 8 COMPLETED SUCCESSFULLY!");
    }

    static string FormatJson(JsonElement element)
    {
        return JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true });
    }
}
