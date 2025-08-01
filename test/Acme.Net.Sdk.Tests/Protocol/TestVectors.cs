using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Provides access to test vectors from the submodule file `test/vectors/protocol.4.json`.
    /// </summary>
    public static class TestVectors
    {
        // Relative path to the vectors file within the submodule, copied to the output directory
        private const string VECTORS_FILE_PATH = "vectors/protocol.4.json";

        private static readonly Lazy<ProtocolTestVectors> _testVectors = new Lazy<ProtocolTestVectors>(() => LoadTestVectors());

        /// <summary>
        /// Gets the loaded test vectors.
        /// </summary>
        public static ProtocolTestVectors Vectors => _testVectors.Value;

        private static ProtocolTestVectors LoadTestVectors()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                throw new InvalidOperationException("Could not determine the test assembly directory.");
            }

            // Combine the assembly directory with the relative path to the vectors file
            string filePath = Path.Combine(assemblyDirectory, VECTORS_FILE_PATH);

            Console.WriteLine($"Attempting to load test vectors from: {filePath}");

            if (!File.Exists(filePath))
            {
                // Provide more context if the file isn't found
                string errorMessage = $"Could not find the test vectors file at {filePath}. " +
                                       "Ensure the 'test/vectors' submodule is initialized and the file '{Path.GetFileName(VECTORS_FILE_PATH)}' exists and is copied to the output directory. " +
                                       "Check the .csproj file configuration.";
                throw new FileNotFoundException(errorMessage, filePath);
            }

            string jsonContent = File.ReadAllText(filePath);
            Console.WriteLine($"Read {jsonContent.Length} bytes from {filePath}");

            try
            {
                var result = JsonSerializer.Deserialize<ProtocolTestVectors>(jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // Keep case-insensitive for robustness
                        AllowTrailingCommas = true
                    });

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize test vectors from JSON file.");
                }

                Console.WriteLine($"Successfully deserialized {result.Transactions.Count} transaction groups from {filePath}");
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing test vectors JSON from {filePath}: {ex.Message}");
                throw new InvalidOperationException($"Error parsing test vectors JSON from {filePath}: {ex.Message}", ex);
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
            
            // Set initiator hash if present
            if (!string.IsNullOrEmpty(txData.Header.Initiator))
            {
                tx.Header.WithInitiator(Hex.DecodeHex(txData.Header.Initiator));
            }
            
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

                case "createidentity":
                    var createIdentity = new CreateIdentity();
                    if (txData.Body.TryGetProperty("url", out var identityUrl))
                        createIdentity.WithUrl(new Url(identityUrl.GetString() ?? string.Empty));
                    if (txData.Body.TryGetProperty("keyHash", out var keyHash))
                    {
                        var keyHashStr = keyHash.GetString() ?? string.Empty;
                        createIdentity.WithKeyHash(Hex.DecodeHex(keyHashStr));
                    }
                    if (txData.Body.TryGetProperty("keyBookUrl", out var keyBookUrl))
                        createIdentity.WithKeyBookUrl(new Url(keyBookUrl.GetString() ?? string.Empty));
                    tx.Body = createIdentity;
                    break;

                case "createdataaccount":
                    var createDataAccount = new CreateDataAccount();
                    if (txData.Body.TryGetProperty("url", out var dataAccountUrl))
                        createDataAccount.WithUrl(new Url(dataAccountUrl.GetString() ?? string.Empty));
                    tx.Body = createDataAccount;
                    break;

                case "acmefaucet":
                    var acmeFaucet = new AcmeFaucet();
                    if (txData.Body.TryGetProperty("url", out var faucetUrl))
                        acmeFaucet.WithUrl(new Url(faucetUrl.GetString() ?? string.Empty));
                    tx.Body = acmeFaucet;
                    break;

                case "createtoken":
                case "createtokenissuer":
                    var createToken = new CreateToken();
                    if (txData.Body.TryGetProperty("url", out var createTokenUrl))
                        createToken.WithUrl(new Url(createTokenUrl.GetString() ?? string.Empty));
                    if (txData.Body.TryGetProperty("symbol", out var symbol))
                        createToken.WithSymbol(symbol.GetString() ?? string.Empty);
                    if (txData.Body.TryGetProperty("precision", out var precision))
                        createToken.WithPrecision(precision.GetInt32());
                    if (txData.Body.TryGetProperty("supplyLimit", out var supplyLimit))
                        createToken.WithSupplyLimit(supplyLimit.GetUInt64());
                    tx.Body = createToken;
                    break;

                case "issuetokens":
                    var issueTokens = new IssueTokens();
                    if (txData.Body.TryGetProperty("recipient", out var issueRecipient))
                        issueTokens.WithRecipient(new Url(issueRecipient.GetString() ?? string.Empty));
                    if (txData.Body.TryGetProperty("amount", out var issueAmount) && issueAmount.GetString() is string issueAmountStr)
                    {
                        var amountBigInt = System.Numerics.BigInteger.Parse(issueAmountStr);
                        issueTokens.WithAmount(amountBigInt);
                    }
                    tx.Body = issueTokens;
                    break;

                case "burntokens":
                    var burnTokens = new BurnTokens();
                    if (txData.Body.TryGetProperty("amount", out var burnAmount) && burnAmount.GetString() is string burnAmountStr)
                        burnTokens.WithAmount(burnAmountStr);
                    tx.Body = burnTokens;
                    break;

                case "addcredits":
                case "issuecredits":
                    var addCredits = new AddCredits();
                    if (txData.Body.TryGetProperty("recipient", out var creditsRecipient))
                        addCredits.WithRecipient(new Url(creditsRecipient.GetString() ?? string.Empty));
                    if (txData.Body.TryGetProperty("amount", out var creditsAmount) && creditsAmount.GetString() is string creditsAmountStr)
                    {
                        var creditsBigInt = System.Numerics.BigInteger.Parse(creditsAmountStr);
                        addCredits.WithAmount(creditsBigInt);
                    }
                    if (txData.Body.TryGetProperty("oracle", out var oracle))
                    {
                        if (oracle.ValueKind == JsonValueKind.Number)
                            addCredits.WithOracle(oracle.GetInt64().ToString());
                        else
                            addCredits.WithOracle(oracle.GetString() ?? string.Empty);
                    }
                    tx.Body = addCredits;
                    break;

                case "createkeypage":
                    var createKeyPage = new CreateKeyPage();
                    if (txData.Body.TryGetProperty("keys", out var keys) && keys.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var key in keys.EnumerateArray())
                        {
                            if (key.TryGetProperty("publicKeyHash", out var pubKeyHash))
                            {
                                var keyHashBytes = Hex.DecodeHex(pubKeyHash.GetString() ?? string.Empty);
                                createKeyPage.AddKey(keyHashBytes);
                            }
                        }
                    }
                    tx.Body = createKeyPage;
                    break;

                case "createkeybook":
                    var createKeyBook = new CreateKeyBook();
                    if (txData.Body.TryGetProperty("url", out var keyBookUrlProp))
                        createKeyBook.WithUrl(new Url(keyBookUrlProp.GetString() ?? string.Empty));
                    tx.Body = createKeyBook;
                    break;

                case "updatekeypage":
                    var updateKeyPage = new UpdateKeyPage();
                    // TODO: Parse operations
                    tx.Body = updateKeyPage;
                    break;

                case "remote":
                case "signpending":
                    var remoteTransaction = new RemoteTransaction();
                    if (txData.Body.TryGetProperty("hash", out var txHash))
                    {
                        var hashBytes = Hex.DecodeHex(txHash.GetString() ?? string.Empty);
                        remoteTransaction.WithHash(hashBytes);
                    }
                    tx.Body = remoteTransaction;
                    break;

                // Synthetic transactions
                case "syntheticcreateidentity":
                case "syntheticwritedata":
                case "syntheticdeposittokens":
                case "syntheticdepositcredits":
                case "syntheticburntokens":
                    // These are synthetic transactions - skip for now as they're not in regular test vectors
                    throw new NotSupportedException($"Synthetic transaction type '{bodyType}' not supported in test parser.");

                // These transaction types are still not implemented
                case "addkeypageoperation":
                case "removekeypageoperation":
                case "updatekeypageoperation":
                case "setkeypageoperationthreshold":
                case "updatetokenissuer":
                    throw new NotSupportedException($"Test case parser for type '{bodyType}' not fully implemented.");

                default:
                     throw new NotSupportedException($"Unsupported transaction body type in test vector: {bodyType}");
            }

            return tx;
        }
        
        /// <summary>
        /// Gets the expected transaction hash from the signature block of the test case.
        /// </summary>
        public static byte[] GetExpectedTransactionHash(this TransactionTestCase testCase)
        {
            // Find the first signature that has a TransactionHash
            var signatureWithHash = testCase.Json.Signatures.FirstOrDefault(s => !string.IsNullOrEmpty(s.TransactionHash));
            if (signatureWithHash != null)
            {
                return Hex.DecodeHex(signatureWithHash.TransactionHash);
            }
            // For SignPending/Remote transactions without a hash, return empty array
            if (testCase.Json.Transaction.Count > 0)
            {
                var bodyType = testCase.Json.Transaction[0].Body.GetProperty("type").GetString() ?? string.Empty;
                if (bodyType.ToLowerInvariant() == "remote" || bodyType.ToLowerInvariant() == "signpending")
                {
                    return new byte[0]; // These may not have transaction hashes in test vectors
                }
            }
            // Fallback or throw if no hash is found (should not happen for valid vectors)
            throw new InvalidOperationException("No signature with a TransactionHash found in the test case.");
        }

        /// <summary>
        /// Gets the expected initiator hash from the header of the test case.
        /// </summary>
        public static byte[] GetExpectedInitiatorHash(this TransactionTestCase testCase)
        {
            if (testCase.Json.Transaction.Count > 0 && !string.IsNullOrEmpty(testCase.Json.Transaction[0].Header.Initiator))
            {
                return Hex.DecodeHex(testCase.Json.Transaction[0].Header.Initiator);
            }
             throw new InvalidOperationException("No initiator hash found in the test case header.");
        }
    }
} 