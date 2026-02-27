using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;

/// <summary>
/// SDK Example 11: Multi-Signature Types (V3)
/// C# port of Python example_11_multi_signature_types.py
///
/// Demonstrates:
/// - All supported signature types in the protocol
/// - Generating Ed25519 key pairs
/// - Signature type enumeration and wire names
/// </summary>
class Program
{
    const string KermitBase = "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 11: Multi-Signature Types (C#) ===\n");
        Console.WriteLine($"Endpoint: {KermitBase}\n");

        using var client = new Accumulate(KermitBase);

        // =========================================================
        // Step 1: List all signature types
        // =========================================================
        Console.WriteLine("--- Step 1: List All Signature Types ---\n");

        foreach (SignatureType st in Enum.GetValues<SignatureType>())
        {
            Console.WriteLine($"  {st} = {(int)st} (wire: \"{st.GetWireName()}\")");
        }
        Console.WriteLine();

        // =========================================================
        // Step 2: Demonstrate wire name lookup
        // =========================================================
        Console.WriteLine("--- Step 2: Wire Name Lookup ---\n");

        string[] wireNames = { "ed25519", "rcd1", "btc", "eth", "delegated", "rsaSha256" };
        foreach (var name in wireNames)
        {
            var sigType = SignatureTypeExtensions.FromWireName(name);
            Console.WriteLine($"  \"{name}\" -> {sigType} (value={(int)sigType})");
        }
        Console.WriteLine();

        // =========================================================
        // Step 3: Generate Ed25519 key pair and sign
        // =========================================================
        Console.WriteLine("--- Step 3: Generate Key Pair ---\n");

        var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var pubHex = Convert.ToHexString(kp.GetPublicKey()).ToLowerInvariant();
        Console.WriteLine($"Generated Ed25519 key pair");
        Console.WriteLine($"  Public key: {pubHex[..32]}...");
        Console.WriteLine($"  Type: {kp.Type} (value={(int)kp.Type})");
        Console.WriteLine($"  Wire name: \"{kp.Type.GetWireName()}\"\n");

        // =========================================================
        // Step 4: Derive lite identity
        // =========================================================
        Console.WriteLine("--- Step 4: Derive Lite Identity ---\n");

        var lid = Principal.ComputeUrl(kp.GetPublicKey());
        var lta = Principal.ComputeUrl(kp.GetPublicKey(), new Url("acc://ACME"));
        Console.WriteLine($"Lite Identity: {lid}");
        Console.WriteLine($"Lite Token Account: {lta}\n");

        // =========================================================
        // Step 5: Fund and query
        // =========================================================
        Console.WriteLine("--- Step 5: Fund & Query ---\n");

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

        await Task.Delay(10000);

        try
        {
            var info = await client.V3.QueryAccountAsync(ltaStr);
            Console.WriteLine($"\nAccount info:\n{JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true })}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nQuery failed: {ex.Message}\n");
        }

        Console.WriteLine("Note: BTC, ETH, RCD1, and other signature types will be");
        Console.WriteLine("fully supported for signing in Phase 2.\n");

        // =========================================================
        // Summary
        // =========================================================
        Console.WriteLine("=== Summary ===\n");
        Console.WriteLine($"Enumerated {Enum.GetValues<SignatureType>().Length} signature types");
        Console.WriteLine("Demonstrated wire name lookup and key generation");
        Console.WriteLine("\nExample 11 COMPLETED SUCCESSFULLY!");
    }
}
