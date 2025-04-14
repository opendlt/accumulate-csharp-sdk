using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Signing; // For SignatureKeyPair, AccKeyPairGenerator, TransactionHasher
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex
using System.Threading.Tasks;

public class BuildEnvelopeExample
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Building and Signing Transaction Envelope...");

        // 1. Load or Generate Principal
        //    In a real app, load your saved key using LiteIdentityPrincipal.ImportFromBase64(savedKeyData);
        var senderPrincipal = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
        Console.WriteLine($"Using sender: {senderPrincipal.LiteIdentity.Url}");

        // 2. Define Transaction Details
        string recipientUrl = "acc://recipient-lite-identity-url/acme"; // Replace with actual recipient
        BigInteger amountToSend = BigInteger.Parse("1000000"); // e.g., 0.01 ACME
        string sourceAccountUrl = senderPrincipal.LiteIdentity.Url + "/acme";

        // 3. Manually Create the Transaction Body
        //    (Instead of using the builder's Execute method)
        var sendTokensBody = new SendTokens();
        sendTokensBody.AddRecipient(recipientUrl, amountToSend);
        // Note: For SendTokens, the source account is implicitly the Principal's URL passed in the header.
        // If the builder did more complex logic, you might need to replicate that here.

        // 4. Create the Transaction Header
        var transactionHeader = new TransactionHeader()
            .WithPrincipal(senderPrincipal.LiteIdentity.Url);

        // 5. Create the Full Transaction Object
        var transaction = new Transaction
        {
            Header = transactionHeader,
            Body = sendTokensBody
        };

        // 6. Compute the Transaction Hash
        byte[] txHash = TransactionHasher.ComputeTransactionHash(transaction);
        string txHashHex = Hex.EncodeHexString(txHash);
        Console.WriteLine($"Computed Transaction Hash: {txHashHex}");

        // 7. Sign the Transaction Hash
        //    Retrieve the key pair from the principal
        SignatureKeyPair keyPair = senderPrincipal.SignatureKeyPair;
        TransactionSignature signature = keyPair.Sign(txHash);
        // The signature object automatically includes PublicKey, Signer URL, Type etc.
        // We need to explicitly set the SignerVersion if it's not the default (1)
        signature.SignerVersion = senderPrincipal.SignerVersion; 

        Console.WriteLine("Transaction Signed.");

        // 8. Build the Envelope
        var envelopeBuilder = new EnvelopeBuilder()
            .AddTransaction(transaction)
            .AddSignature(signature)
            .SetTxHash(txHashHex); // Include the hash in the envelope for clarity

        var envelope = envelopeBuilder.Build();

        // 9. Serialize the Envelope to JSON
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Match standard JSON conventions
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Don't include null fields
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)); // Serialize enums as strings
        // Custom converter might be needed for BigInteger if not handled by default

        string envelopeJson = JsonSerializer.Serialize(envelope, jsonOptions);

        // 10. Print the JSON Envelope
        Console.WriteLine("\n--- JSON Envelope --- ");
        Console.WriteLine(envelopeJson);
        Console.WriteLine("--- End JSON Envelope ---");
    }
} 