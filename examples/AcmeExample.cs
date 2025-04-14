using System;
using System.Threading.Tasks;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Signing;

namespace AcmeExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Acme.Net SDK Example");
            Console.WriteLine("====================");

            try
            {
                // 1. Create client
                Console.WriteLine("Creating Acme client...");
                var client = new AcmeClient("https://testnet.accumulatenetwork.io/v2");

                // 2. Generate a key pair
                Console.WriteLine("Generating a new ED25519 key pair...");
                var liteIdentity = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
                Console.WriteLine($"Generated lite identity URL: {liteIdentity.LiteIdentity.Url}");

                // Optional: Export the key if you want to save it for later
                string exportedKey = liteIdentity.ExportToBase64();
                Console.WriteLine($"Exported key (save this for later use): {exportedKey}");

                // 3. Create a token account for ACME tokens
                Console.WriteLine("\nCreating a token account...");
                var acmeTokenUrl = UrlRegistry.GetInstance().GetAcmeTokenUrl();
                
                // Create a token account builder
                var createTokenAccountTx = client.CreateTokenAccountBuilder()
                    .WithOrigin(liteIdentity.LiteIdentity.Url)  // The lite identity will initiate this transaction
                    .WithSigner(liteIdentity)                  // And sign it
                    .WithTokenUrl(acmeTokenUrl)                // For ACME tokens
                    .WithAccountUrl($"{liteIdentity.LiteIdentity.Url}/acme"); // Sub-URL for the token account
                
                // 4. Execute the transaction
                Console.WriteLine("Executing the transaction...");
                try
                {
                    var response = await createTokenAccountTx.ExecuteSignedAsync();
                    
                    // 5. Check the result
                    Console.WriteLine("\nTransaction response:");
                    Console.WriteLine($"Transaction ID: {response.TxId}");
                    Console.WriteLine($"Success: {!string.IsNullOrEmpty(response.TxId)}");
                    
                    if (!string.IsNullOrEmpty(response.Error))
                    {
                        Console.WriteLine($"Error: {response.Error}");
                    }
                    else
                    {
                        Console.WriteLine("Transaction submitted successfully!");
                        Console.WriteLine($"Token account created at: {liteIdentity.LiteIdentity.Url}/acme");
                    }
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"RPC Error: {ex.Message}");
                    if (ex.Data.Contains("Response"))
                    {
                        Console.WriteLine($"Response details: {ex.Data["Response"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
} 