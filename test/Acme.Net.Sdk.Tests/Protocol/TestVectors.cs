using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Provides access to test vectors from the protocol.4.json file.
    /// </summary>
    public static class TestVectors
    {
        private static readonly Lazy<ProtocolTestVectors> _testVectors = new Lazy<ProtocolTestVectors>(() => LoadTestVectors());

        /// <summary>
        /// Gets the loaded test vectors.
        /// </summary>
        public static ProtocolTestVectors Vectors => _testVectors.Value;

        private static ProtocolTestVectors LoadTestVectors()
        {
            // Use direct absolute path to the file
            string filePath = "/home/bunfield/Software/sdk/acme.net/test-data/protocol.4.json";
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Could not find protocol.4.json at {filePath}");
            }
            
            string jsonContent = File.ReadAllText(filePath);
            Console.WriteLine($"Read {jsonContent.Length} bytes from {filePath}");
            
            try
            {
                var result = JsonSerializer.Deserialize<ProtocolTestVectors>(jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });
                
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize test vectors");
                }
                
                Console.WriteLine($"Deserialized {result.Transactions.Count} transaction groups");
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing test vectors: {ex.Message}");
                throw new InvalidOperationException($"Error parsing test vectors: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Represents the structure of the protocol test vectors.
    /// </summary>
    public class ProtocolTestVectors
    {
        public List<TransactionTestGroup> Transactions { get; set; } = new List<TransactionTestGroup>();
    }

    /// <summary>
    /// Represents a group of test cases for a specific transaction type.
    /// </summary>
    public class TransactionTestGroup
    {
        public string Name { get; set; } = string.Empty;
        public List<TransactionTestCase> Cases { get; set; } = new List<TransactionTestCase>();
    }

    /// <summary>
    /// Represents a single test case with binary and JSON representations.
    /// </summary>
    public class TransactionTestCase
    {
        public string Binary { get; set; } = string.Empty;
        public TransactionTestJson Json { get; set; } = new TransactionTestJson();
    }

    /// <summary>
    /// Represents the JSON part of a test case.
    /// </summary>
    public class TransactionTestJson
    {
        public List<TransactionTestSignature> Signatures { get; set; } = new List<TransactionTestSignature>();
        public List<TransactionTestData> Transaction { get; set; } = new List<TransactionTestData>();
    }

    /// <summary>
    /// Represents a signature in a test case.
    /// </summary>
    public class TransactionTestSignature
    {
        public string Type { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string Signer { get; set; } = string.Empty;
        public int SignerVersion { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a transaction in a test case.
    /// </summary>
    public class TransactionTestData
    {
        public TransactionTestHeader Header { get; set; } = new TransactionTestHeader();
        public JsonElement Body { get; set; }
    }

    /// <summary>
    /// Represents a transaction header in a test case.
    /// </summary>
    public class TransactionTestHeader
    {
        public string Principal { get; set; } = string.Empty;
        public string Initiator { get; set; } = string.Empty;
    }

    /// <summary>
    /// Extension methods for working with test vectors.
    /// </summary>
    public static class TestVectorExtensions
    {
        /// <summary>
        /// Converts a test case to a Transaction object.
        /// </summary>
        public static Transaction ToTransaction(this TransactionTestCase testCase)
        {
            if (testCase.Json.Transaction.Count == 0)
                throw new ArgumentException("No transaction data found in test case");

            var txData = testCase.Json.Transaction[0];
            
            var tx = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url(txData.Header.Principal))
            };
            
            // Get the reference hash from the signature if available
            if (testCase.Json.Signatures.Count > 0 && !string.IsNullOrEmpty(testCase.Json.Signatures[0].TransactionHash))
            {
                tx.Hash = Hex.DecodeHex(testCase.Json.Signatures[0].TransactionHash);
            }
            
            // Set body based on type property in the JSON
            string bodyType = txData.Body.GetProperty("type").GetString() ?? string.Empty;
            
            switch (bodyType.ToLowerInvariant())
            {
                case "createtokenaccount":
                    var createTokenAccount = new CreateTokenAccount();
                    if (txData.Body.TryGetProperty("url", out var url))
                        createTokenAccount.WithUrl(new Url(url.GetString() ?? string.Empty));
                    
                    if (txData.Body.TryGetProperty("tokenUrl", out var tokenUrl))
                        createTokenAccount.WithTokenUrl(new Url(tokenUrl.GetString() ?? string.Empty));

                    // Handle authorities - converting first authority to keyBookUrl if present
                    if (txData.Body.TryGetProperty("authorities", out var authorities) && authorities.ValueKind == JsonValueKind.Array)
                    {
                        var authArray = authorities.EnumerateArray();
                        if (authArray.Any())
                        {
                            var firstAuth = authArray.First().GetString();
                            if (!string.IsNullOrEmpty(firstAuth))
                            {
                                createTokenAccount.WithKeyBookUrl(new Url(firstAuth));
                            }
                        }
                    }

                    tx.Body = createTokenAccount;
                    break;

                case "sendtokens":
                    var sendTokens = new SendTokens();
                    if (txData.Body.TryGetProperty("to", out var recipients) && recipients.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var recipientObj in recipients.EnumerateArray())
                        {
                            string? recipientUrl = null;
                            ulong amount = 0;
                            
                            if (recipientObj.TryGetProperty("url", out var recipientUrlProp))
                                recipientUrl = recipientUrlProp.GetString();
                            
                            if (recipientObj.TryGetProperty("amount", out var amountProp) && amountProp.GetString() is string amountStr)
                                ulong.TryParse(amountStr, out amount);

                            if (!string.IsNullOrEmpty(recipientUrl))
                                sendTokens.AddRecipient(recipientUrl, amount);
                        }
                    }
                    tx.Body = sendTokens;
                    break;

                case "writedata":
                    var writeData = new WriteData();
                    if (txData.Body.TryGetProperty("entry", out var entry) && 
                        entry.TryGetProperty("data", out var data) && 
                        data.ValueKind == JsonValueKind.Array)
                    {
                        var dataBytes = new List<byte>();
                        foreach (var item in data.EnumerateArray())
                        {
                            var itemString = item.GetString() ?? string.Empty;
                            var itemBytes = Hex.DecodeHex(itemString);
                            dataBytes.AddRange(itemBytes);
                        }
                        writeData.WithData(dataBytes.ToArray());
                    }
                    tx.Body = writeData;
                    break;

                case "writedatato":
                    var writeDataTo = new WriteDataTo();
                    if (txData.Body.TryGetProperty("recipient", out var recipientProp))
                        writeDataTo.WithRecipient(new Url(recipientProp.GetString() ?? string.Empty));
                    
                    if (txData.Body.TryGetProperty("entry", out var entryTo) && 
                        entryTo.TryGetProperty("data", out var dataTo) && 
                        dataTo.ValueKind == JsonValueKind.Array)
                    {
                        var dataBytes = new List<byte>();
                        foreach (var item in dataTo.EnumerateArray())
                        {
                            var itemString = item.GetString() ?? string.Empty;
                            var itemBytes = Hex.DecodeHex(itemString);
                            dataBytes.AddRange(itemBytes);
                        }
                        writeDataTo.WithData(dataBytes.ToArray());
                    }
                    tx.Body = writeDataTo;
                    break;

                // These transaction types are not yet implemented in the SDK
                case "createdataaccount":
                case "createidentity":
                case "syntheticwritedata":
                    throw new NotSupportedException($"Transaction type '{bodyType}' is not yet implemented in the SDK");

                default:
                    throw new NotSupportedException($"Transaction type '{bodyType}' is not supported");
            }

            return tx;
        }

        /// <summary>
        /// Gets the expected transaction hash for a test case.
        /// </summary>
        public static byte[] GetExpectedTransactionHash(this TransactionTestCase testCase)
        {
            if (testCase.Json.Signatures.Count == 0)
                throw new ArgumentException("No signatures found in test case");

            // Find the proper signature that contains the transaction hash
            var signature = testCase.Json.Signatures[0];
            
            // Make sure we have a transaction hash
            if (string.IsNullOrEmpty(signature.TransactionHash))
                throw new ArgumentException("No transaction hash found in signature");

            return Hex.DecodeHex(signature.TransactionHash);
        }

        /// <summary>
        /// Gets the expected initiator hash for a test case.
        /// </summary>
        public static byte[] GetExpectedInitiatorHash(this TransactionTestCase testCase)
        {
            if (testCase.Json.Transaction.Count == 0)
                throw new ArgumentException("No transaction data found in test case");

            string initiatorHashHex = testCase.Json.Transaction[0].Header.Initiator;
            return Hex.DecodeHex(initiatorHashHex);
        }
    }
} 