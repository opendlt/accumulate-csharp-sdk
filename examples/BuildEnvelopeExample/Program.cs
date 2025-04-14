using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Signing; // For AccKeyPairGenerator, TransactionHasher, Signer, ISignature, etc.
// using Acme.Net.Sdk.Protocol.Signing; // Keep commented or remove if TransactionSignature not needed elsewhere
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex
using System.Threading.Tasks;

public class BuildEnvelopeExample
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Building and Signing Transaction Envelope...");

        // 1. Load or Generate Principal (which also acts as a Signer)
        //    In a real app, load your saved key using LiteIdentityPrincipal.ImportFromBase64(savedKeyData);
        var senderPrincipal = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
        Console.WriteLine($"Using sender: {senderPrincipal.LiteIdentity.Url}");

        // 2. Define Transaction Details
        string recipientUrlString = "acc://recipient-lite-identity-url/acme"; // Replace with actual recipient
        BigInteger amountToSend = BigInteger.Parse("1000000"); // e.g., 0.01 ACME
        string sourceAccountUrl = senderPrincipal.LiteIdentity.Url + "/acme"; // Needed for body

        // 3. Manually Create the Transaction Body
        var sendTokensBody = new SendTokens();
        sendTokensBody.AddRecipient(new Url(recipientUrlString), (ulong)amountToSend);

        // 4. Create the Transaction Header (using the Principal's URL)
        var transactionHeader = new TransactionHeader()
            .WithPrincipal(senderPrincipal.LiteIdentity.Url);

        // 5. Create the Full Transaction Object
        var transaction = new Transaction
        {
            Header = transactionHeader,
            Body = sendTokensBody
        };

        // 6. Configure a Signer using the Principal's details
        var signer = new Signer()
            .WithUrl(senderPrincipal.LiteIdentity.Url) // Set the signer URL
            .WithKeyPair(senderPrincipal.SignatureKeyPair) // Set the key pair
            .WithVersion(senderPrincipal.SignerVersion) // Set the version (usually 1)
            .WithType(senderPrincipal.SignatureKeyPair.Type) // Set the type from the keypair
            .WithNonceFromTimeNow(); // Generate timestamp/nonce

        // 7. Use the configured Signer to initiate the transaction.
        //    This computes hashes, sets initiator hash, and signs internally.
        // Explicitly qualify ISignature to resolve ambiguity
        Acme.Net.Sdk.Signing.ISignature signature = signer.Initiate(transaction);

        Console.WriteLine("Transaction Signing Initiated.");

        // The transaction hash is computed internally by Initiate, but we can recompute for verification/display
        byte[] txHash = TransactionHasher.ComputeTransactionHash(transaction);
        string txHashHex = Hex.EncodeHexString(txHash);
        Console.WriteLine($"Transaction Hash: {txHashHex}"); // Note: Header.Initiator was set by Initiate() call

        // 8. Build the Envelope using the ISignature from Initiate()
        var envelopeBuilder = new EnvelopeBuilder()
            .AddTransaction(transaction)
            .AddSignature(signature) // Add the ISignature object returned by Initiate()
            .SetTxHash(txHashHex); // Optionally include the hash hex string

        var envelope = envelopeBuilder.Build();

        // 9. Serialize the Envelope to JSON
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Match standard JSON conventions
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Don't include null fields
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)); // Serialize enums as strings

        string envelopeJson = JsonSerializer.Serialize(envelope, jsonOptions);

        // 10. Print the JSON Envelope
        Console.WriteLine("\n--- JSON Envelope --- ");
        Console.WriteLine(envelopeJson);
        Console.WriteLine("--- End JSON Envelope ---");
    }
} 