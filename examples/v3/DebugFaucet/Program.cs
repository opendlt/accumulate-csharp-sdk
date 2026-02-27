using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;

/// <summary>
/// Debug script to test faucet and query functionality.
/// C# port of Python debug_faucet.py
/// </summary>
class Program
{
    // For Kermit testnet:
    const string KermitBase = "https://kermit.accumulatenetwork.io";

    // For local DevNet testing, uncomment:
    // const string KermitBase = "http://127.0.0.1:26660";

    static async Task Main()
    {
        // Generate keypair
        var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var lid = Principal.ComputeUrl(kp.GetPublicKey());
        var lta = Principal.ComputeUrl(kp.GetPublicKey(), new Url("acc://ACME"));

        Console.WriteLine($"Lite Identity: {lid}");
        Console.WriteLine($"Lite Token Account: {lta}");

        using var client = new Accumulate(KermitBase);
        var ltaStr = lta.String();

        // Try faucet with V2
        Console.WriteLine("\n--- Testing Faucet (V2) ---");
        try
        {
            var faucetResult = await client.V2.FaucetAsync(ltaStr);
            Console.WriteLine($"Faucet response: {JsonSerializer.Serialize(faucetResult, new JsonSerializerOptions { WriteIndented = true })}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Faucet error: {ex.Message}");
        }

        Console.WriteLine("\nWaiting 20 seconds for transaction to process...");
        await Task.Delay(20000);

        // Try querying the account with V3
        Console.WriteLine("\n--- Testing Query (V3) ---");
        try
        {
            var result = await client.V3.QueryAccountAsync(ltaStr);
            Console.WriteLine($"V3 Query result: {JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })}");

            if (result.TryGetProperty("account", out var account) &&
                account.TryGetProperty("balance", out var balance))
            {
                Console.WriteLine($"Balance from V3: {balance}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"V3 Query error: {ex.GetType().Name}: {ex.Message}");
        }

        // Try V2 query
        Console.WriteLine("\n--- Testing Query (V2) ---");
        try
        {
            var v2Result = await client.V2.QueryAsync(ltaStr);
            Console.WriteLine($"V2 Query result: {JsonSerializer.Serialize(v2Result, new JsonSerializerOptions { WriteIndented = true })}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"V2 Query error: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine("\n--- Summary ---");
        Console.WriteLine("If the faucet returned a txid but balance is 0 or error,");
        Console.WriteLine("it may take more time for the transaction to settle.");
        Console.WriteLine("The Kermit testnet may also be slow or congested.");
    }
}
