using System;
using System.Text;
using System.Threading.Tasks;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Rpc;
using Acme.Net.Sdk.Signing;
using System.Numerics;

namespace AcmeComplexExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Acme.Net SDK Advanced Example");
            Console.WriteLine("=============================");

            try
            {
                // 1. Create client
                Console.WriteLine("Creating Acme client...");
                var client = new AcmeClient("https://testnet.accumulatenetwork.io/v2");

                // 2. Generate two key pairs
                Console.WriteLine("Generating key pairs...");
                var sender = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
                var recipient = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
                
                Console.WriteLine($"Sender identity URL: {sender.LiteIdentity.Url}");
                Console.WriteLine($"Recipient identity URL: {recipient.LiteIdentity.Url}");

                // 3. Create token accounts for both identities
                Console.WriteLine("\nCreating token accounts...");
                var acmeTokenUrl = UrlRegistry.GetInstance().GetAcmeTokenUrl();
                
                var senderTokenAccountUrl = $"{sender.LiteIdentity.Url}/acme";
                var recipientTokenAccountUrl = $"{recipient.LiteIdentity.Url}/acme";
                
                Console.WriteLine($"Creating sender token account at {senderTokenAccountUrl}");
                await CreateTokenAccount(client, sender, acmeTokenUrl, senderTokenAccountUrl);
                
                Console.WriteLine($"Creating recipient token account at {recipientTokenAccountUrl}");
                await CreateTokenAccount(client, recipient, acmeTokenUrl, recipientTokenAccountUrl);
                
                // 4. Add credits to the sender account
                Console.WriteLine("\nAdding credits to sender account...");
                await AddCredits(client, sender);
                
                // 5. Create a data account for the sender
                Console.WriteLine("\nCreating data account for sender...");
                var dataAccountUrl = $"{sender.LiteIdentity.Url}/data";
                await CreateDataAccount(client, sender, dataAccountUrl);
                
                // 6. Write data to the data account
                Console.WriteLine("\nWriting data to the data account...");
                await WriteData(client, sender, dataAccountUrl, "Hello, Accumulate!");
                
                // 7. Send tokens from sender to recipient
                Console.WriteLine("\nSending tokens from sender to recipient...");
                await SendTokens(client, sender, senderTokenAccountUrl, recipientTokenAccountUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        private static async Task CreateTokenAccount(AcmeClient client, LiteIdentityPrincipal principal, Url tokenUrl, string accountUrl)
        {
            try
            {
                var createTokenAccountTx = client.CreateTokenAccountBuilder()
                    .WithOrigin(principal.LiteIdentity.Url)
                    .WithSigner(principal)
                    .WithTokenUrl(tokenUrl)
                    .WithAccountUrl(accountUrl);
                
                var response = await createTokenAccountTx.ExecuteSignedAsync();
                
                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Error creating token account: {response.Error}");
                }
                else
                {
                    Console.WriteLine($"Token account created. Transaction ID: {response.TxId}");
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
        
        private static async Task AddCredits(AcmeClient client, LiteIdentityPrincipal principal)
        {
            try
            {
                // In a real application, you would use tokens from a funded account
                // For testnet, we can use the faucet
                var addCreditsTx = client.CreateAddCreditsBuilder()
                    .WithOrigin(principal.LiteIdentity.Url)
                    .WithSigner(principal)
                    .WithRecipient(principal.LiteIdentity.Url)
                    .WithAmount(100); // Amount in ACME tokens (would be converted to credits)
                
                var response = await addCreditsTx.ExecuteSignedAsync();
                
                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Error adding credits: {response.Error}");
                }
                else
                {
                    Console.WriteLine($"Credits added. Transaction ID: {response.TxId}");
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
        
        private static async Task CreateDataAccount(AcmeClient client, LiteIdentityPrincipal principal, string dataAccountUrl)
        {
            try
            {
                // Create a data account
                var accountsClient = client.Accounts();
                var createDataAccountBody = new CreateDataAccount()
                    .WithUrl(new Url(dataAccountUrl));
                
                // Create the transaction
                var transaction = new Transaction()
                    .WithHeader(new TransactionHeader()
                        .WithPrincipal(principal.LiteIdentity.Url))
                    .WithBody(createDataAccountBody);
                
                // Sign the transaction using the principal
                var signature = principal.Initiate(transaction);
                
                // Build the envelope with the transaction and signature
                var envelope = new EnvelopeBuilder()
                    .AddTransaction(transaction)
                    .AddSignature(signature)
                    .Build();
                
                var response = await accountsClient.ExecuteAsync(envelope);
                
                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Error creating data account: {response.Error}");
                }
                else
                {
                    Console.WriteLine($"Data account created. Transaction ID: {response.TxId}");
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
        
        private static async Task WriteData(AcmeClient client, LiteIdentityPrincipal principal, string dataAccountUrl, string data)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                
                var writeDataTx = client.WriteDataBuilder()
                    .WithOrigin(principal.LiteIdentity.Url)
                    .WithSigner(principal)
                    .WithData(dataBytes);
                
                var response = await writeDataTx.ExecuteSignedAsync();
                
                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Error writing data: {response.Error}");
                }
                else
                {
                    Console.WriteLine($"Data written. Transaction ID: {response.TxId}");
                    Console.WriteLine($"Data: {data}");
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
        
        private static async Task SendTokens(AcmeClient client, LiteIdentityPrincipal sender, string senderAccountUrl, string recipientAccountUrl)
        {
            try
            {
                var sendTokensTx = client.CreateSendTokensBuilder()
                    .WithOrigin(sender.LiteIdentity.Url)
                    .WithSigner(sender)
                    .WithSourceUrl(senderAccountUrl)
                    .WithRecipientUrl(recipientAccountUrl)
                    .WithAmount(BigInteger.Parse("1000000")); // 0.01 ACME (assuming 8 decimal places)
                
                var response = await sendTokensTx.ExecuteSignedAsync();
                
                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Error sending tokens: {response.Error}");
                }
                else
                {
                    Console.WriteLine($"Tokens sent. Transaction ID: {response.TxId}");
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
    }
} 