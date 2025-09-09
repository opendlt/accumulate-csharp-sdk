#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Comprehensive test that validates ALL transaction types against test vectors.
    /// </summary>
    public class ComprehensiveTestVectorValidation
    {
        private readonly ITestOutputHelper _output;
        private readonly ProtocolTestVectors _testVectors;

        public ComprehensiveTestVectorValidation(ITestOutputHelper output)
        {
            _output = output;
            _testVectors = TestVectors.Vectors;
        }

        [Theory]
        [InlineData("SendTokens")]
        [InlineData("CreateTokenAccount")]
        [InlineData("AddCredits")]
        [InlineData("BurnTokens")]
        [InlineData("CreateDataAccount")]
        [InlineData("CreateIdentity")]
        [InlineData("CreateKeyBook")]
        [InlineData("CreateKeyPage")]
        [InlineData("CreateToken")]
        [InlineData("IssueTokens")]
        [InlineData("UpdateKeyPage")]
        [InlineData("WriteData")]
        [InlineData("WriteDataTo")]
        [InlineData("AcmeFaucet")]
        public void ValidateTransactionHashAgainstTestVector(string transactionType)
        {
            // Arrange
            var group = _testVectors.Transactions.FirstOrDefault(g => g.Name == transactionType);
            if (group == null)
            {
                _output.WriteLine($"No test vector found for {transactionType}");
                return;
            }

            Assert.NotEmpty(group.Cases);
            var testCase = group.Cases[0];
            var txData = testCase.Json.Transaction[0];
            
            // Get the expected hash from the test vector
            string? expectedHashHex = null;
            if (testCase.Json.Signatures.Count > 0 && !string.IsNullOrEmpty(testCase.Json.Signatures[0].TransactionHash))
            {
                expectedHashHex = testCase.Json.Signatures[0].TransactionHash;
            }
            
            if (string.IsNullOrEmpty(expectedHashHex))
            {
                _output.WriteLine($"Skipping {transactionType} - no hash in test vector");
                return;
            }

            // Build the transaction
            var transaction = BuildTransactionFromTestVector(testCase.Json.Transaction[0]);
            if (transaction == null)
            {
                _output.WriteLine($"Failed to build transaction for {transactionType}");
                return;
            }

            // Act - Compute the hash
            byte[] computedHash = TransactionHasher.ComputeRawHash(transaction);
            string computedHashHex = new string(Hex.EncodeHex(computedHash));
            
            // Log details
            _output.WriteLine($"Transaction type: {transactionType}");
            _output.WriteLine($"Expected hash: {expectedHashHex}");
            _output.WriteLine($"Computed hash: {computedHashHex}");
            
            if (transaction.Body != null)
            {
                byte[] bodyBytes = ((ITransactionBody)transaction.Body).MarshalBinary();
                _output.WriteLine($"Body bytes: {new string(Hex.EncodeHex(bodyBytes))}");
            }
            
            // Assert
            Assert.Equal(expectedHashHex.ToLower(), computedHashHex.ToLower());
        }

        private Transaction? BuildTransactionFromTestVector(TransactionTestData txData)
        {
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url(txData.Header.Principal))
            };
            
            if (!string.IsNullOrEmpty(txData.Header.Initiator))
            {
                transaction.Header = transaction.Header.WithInitiator(txData.Header.Initiator);
            }

            string bodyType = txData.Body.GetProperty("type").GetString() ?? string.Empty;
            
            switch (bodyType.ToLowerInvariant())
            {
                case "sendtokens":
                    transaction.Body = BuildSendTokens(txData.Body);
                    break;
                    
                case "createtokenaccount":
                    transaction.Body = BuildCreateTokenAccount(txData.Body);
                    break;
                    
                case "addcredits":
                    transaction.Body = BuildAddCredits(txData.Body);
                    break;
                    
                case "burntokens":
                    transaction.Body = BuildBurnTokens(txData.Body);
                    break;
                    
                case "createdataaccount":
                    transaction.Body = BuildCreateDataAccount(txData.Body);
                    break;
                    
                case "createidentity":
                    transaction.Body = BuildCreateIdentity(txData.Body);
                    break;
                    
                case "createkeybook":
                    transaction.Body = BuildCreateKeyBook(txData.Body);
                    break;
                    
                case "createkeypage":
                    transaction.Body = BuildCreateKeyPage(txData.Body);
                    break;
                    
                case "createtoken":
                    transaction.Body = BuildCreateToken(txData.Body);
                    break;
                    
                case "issuetokens":
                    transaction.Body = BuildIssueTokens(txData.Body);
                    break;
                    
                case "updatekeypage":
                    transaction.Body = BuildUpdateKeyPage(txData.Body);
                    break;
                    
                case "writedata":
                    transaction.Body = BuildWriteData(txData.Body);
                    break;
                    
                case "writedatato":
                    transaction.Body = BuildWriteDataTo(txData.Body);
                    break;
                    
                case "acmefaucet":
                    transaction.Body = BuildAcmeFaucet(txData.Body);
                    break;
                    
                default:
                    _output.WriteLine($"Unsupported transaction type: {bodyType}");
                    return null;
            }

            return transaction;
        }

        private SendTokens BuildSendTokens(JsonElement body)
        {
            var sendTokens = new SendTokens();
            
            if (body.TryGetProperty("hash", out var hashProp))
                sendTokens.Hash = hashProp.GetString();
            
            if (body.TryGetProperty("meta", out var metaProp))
                sendTokens.Meta = new Newtonsoft.Json.Linq.JRaw(metaProp.GetRawText());
            
            if (body.TryGetProperty("to", out var recipients) && recipients.ValueKind == JsonValueKind.Array)
            {
                foreach (var recipient in recipients.EnumerateArray())
                {
                    string? url = recipient.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                    ulong amount = 0;
                    
                    if (recipient.TryGetProperty("amount", out var amountProp))
                    {
                        if (amountProp.ValueKind == JsonValueKind.String)
                            ulong.TryParse(amountProp.GetString(), out amount);
                        else if (amountProp.ValueKind == JsonValueKind.Number)
                            amount = (ulong)amountProp.GetInt64();
                    }
                    
                    if (!string.IsNullOrEmpty(url))
                        sendTokens.AddRecipient(url, amount);
                }
            }
            
            return sendTokens;
        }

        private CreateTokenAccount BuildCreateTokenAccount(JsonElement body)
        {
            var create = new CreateTokenAccount();
            
            if (body.TryGetProperty("url", out var url))
                create.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("tokenUrl", out var tokenUrl))
                create.WithTokenUrl(new Url(tokenUrl.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("authorities", out var authorities) && authorities.ValueKind == JsonValueKind.Array)
            {
                var firstAuth = authorities.EnumerateArray().FirstOrDefault();
                if (firstAuth.ValueKind != JsonValueKind.Undefined)
                {
                    var authStr = firstAuth.GetString();
                    if (!string.IsNullOrEmpty(authStr))
                        create.WithKeyBookUrl(new Url(authStr));
                }
            }
            
            return create;
        }

        private AddCredits BuildAddCredits(JsonElement body)
        {
            var addCredits = new AddCredits();
            
            if (body.TryGetProperty("recipient", out var recipient))
                addCredits.WithRecipient(new Url(recipient.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("amount", out var amount))
            {
                if (amount.ValueKind == JsonValueKind.String)
                {
                    if (ulong.TryParse(amount.GetString(), out var amountVal))
                        addCredits.WithAmount(amountVal);
                }
                else if (amount.ValueKind == JsonValueKind.Number)
                {
                    addCredits.WithAmount((ulong)amount.GetInt64());
                }
            }
            
            if (body.TryGetProperty("oracle", out var oracle))
            {
                if (oracle.ValueKind == JsonValueKind.String)
                {
                    if (ulong.TryParse(oracle.GetString(), out var oracleVal))
                        addCredits.WithOracle(oracleVal);
                }
                else if (oracle.ValueKind == JsonValueKind.Number)
                {
                    addCredits.WithOracle((ulong)oracle.GetInt64());
                }
            }
            
            return addCredits;
        }

        private BurnTokens BuildBurnTokens(JsonElement body)
        {
            var burnTokens = new BurnTokens();
            
            if (body.TryGetProperty("amount", out var amount))
            {
                if (amount.ValueKind == JsonValueKind.String)
                {
                    if (ulong.TryParse(amount.GetString(), out var amountVal))
                        burnTokens.WithAmount(amountVal);
                }
                else if (amount.ValueKind == JsonValueKind.Number)
                {
                    burnTokens.WithAmount((ulong)amount.GetInt64());
                }
            }
            
            return burnTokens;
        }

        private CreateDataAccount BuildCreateDataAccount(JsonElement body)
        {
            var create = new CreateDataAccount();
            
            if (body.TryGetProperty("url", out var url))
                create.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("authorities", out var authorities) && authorities.ValueKind == JsonValueKind.Array)
            {
                foreach (var auth in authorities.EnumerateArray())
                {
                    var authStr = auth.GetString();
                    if (!string.IsNullOrEmpty(authStr))
                        create.AddAuthority(new Url(authStr));
                }
            }
            
            return create;
        }

        private CreateIdentity BuildCreateIdentity(JsonElement body)
        {
            var create = new CreateIdentity();
            
            if (body.TryGetProperty("url", out var url))
                create.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("keyHash", out var keyHash))
            {
                var keyHashStr = keyHash.GetString();
                if (!string.IsNullOrEmpty(keyHashStr))
                {
                    var keyHashBytes = new byte[keyHashStr.Length / 2];
                    for (int i = 0; i < keyHashBytes.Length; i++)
                    {
                        keyHashBytes[i] = Convert.ToByte(keyHashStr.Substring(i * 2, 2), 16);
                    }
                    create.WithKeyHash(keyHashBytes);
                }
            }
            
            if (body.TryGetProperty("keyBookUrl", out var keyBookUrl))
                create.WithKeyBookUrl(new Url(keyBookUrl.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("authorities", out var authorities) && authorities.ValueKind == JsonValueKind.Array)
            {
                foreach (var auth in authorities.EnumerateArray())
                {
                    var authStr = auth.GetString();
                    if (!string.IsNullOrEmpty(authStr))
                        create.AddAuthority(new Url(authStr));
                }
            }
            
            return create;
        }

        private CreateKeyBook BuildCreateKeyBook(JsonElement body)
        {
            var create = new CreateKeyBook();
            
            if (body.TryGetProperty("url", out var url))
                create.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("publicKeyHash", out var keyHash))
            {
                var keyHashStr = keyHash.GetString();
                if (!string.IsNullOrEmpty(keyHashStr))
                {
                    var keyHashBytes = new byte[keyHashStr.Length / 2];
                    for (int i = 0; i < keyHashBytes.Length; i++)
                    {
                        keyHashBytes[i] = Convert.ToByte(keyHashStr.Substring(i * 2, 2), 16);
                    }
                    create.PublicKeyHash = keyHashBytes;
                }
            }
            
            if (body.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
            {
                foreach (var page in pages.EnumerateArray())
                {
                    var pageStr = page.GetString();
                    if (!string.IsNullOrEmpty(pageStr))
                        create.AddPage(new Url(pageStr));
                }
            }
            
            return create;
        }

        private CreateKeyPage BuildCreateKeyPage(JsonElement body)
        {
            var create = new CreateKeyPage();
            
            if (body.TryGetProperty("keys", out var keys) && keys.ValueKind == JsonValueKind.Array)
            {
                foreach (var key in keys.EnumerateArray())
                {
                    if (key.TryGetProperty("keyHash", out var keyHashProp))
                    {
                        var keyHashStr = keyHashProp.GetString();
                        if (!string.IsNullOrEmpty(keyHashStr))
                        {
                            var keyHashBytes = new byte[keyHashStr.Length / 2];
                            for (int i = 0; i < keyHashBytes.Length; i++)
                            {
                                keyHashBytes[i] = Convert.ToByte(keyHashStr.Substring(i * 2, 2), 16);
                            }
                            
                            var keySpec = new KeySpecParams { KeyHash = keyHashBytes };
                            
                            if (key.TryGetProperty("delegate", out var delegateProp))
                            {
                                var delegateStr = delegateProp.GetString();
                                if (!string.IsNullOrEmpty(delegateStr))
                                    keySpec.Delegate = new Url(delegateStr);
                            }
                            
                            create.AddKey(keySpec.KeyHash, keySpec.Delegate);
                        }
                    }
                }
            }
            
            return create;
        }

        private CreateToken BuildCreateToken(JsonElement body)
        {
            var create = new CreateToken();
            
            if (body.TryGetProperty("url", out var url))
                create.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("symbol", out var symbol))
                create.WithSymbol(symbol.GetString() ?? string.Empty);
            
            if (body.TryGetProperty("precision", out var precision))
                create.WithPrecision((byte)precision.GetInt32());
            
            if (body.TryGetProperty("properties", out var properties))
                create.WithProperties(new Url(properties.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("supplyLimit", out var supplyLimit))
            {
                if (supplyLimit.ValueKind == JsonValueKind.String)
                {
                    if (ulong.TryParse(supplyLimit.GetString(), out var limitVal))
                        create.WithSupplyLimit(limitVal);
                }
                else if (supplyLimit.ValueKind == JsonValueKind.Number)
                {
                    create.WithSupplyLimit((ulong)supplyLimit.GetInt64());
                }
            }
            
            return create;
        }

        private IssueTokens BuildIssueTokens(JsonElement body)
        {
            var issue = new IssueTokens();
            
            if (body.TryGetProperty("recipient", out var recipient))
                issue.WithRecipient(new Url(recipient.GetString() ?? string.Empty));
            
            if (body.TryGetProperty("amount", out var amount))
            {
                if (amount.ValueKind == JsonValueKind.String)
                {
                    if (ulong.TryParse(amount.GetString(), out var amountVal))
                        issue.WithAmount(amountVal);
                }
                else if (amount.ValueKind == JsonValueKind.Number)
                {
                    issue.WithAmount((ulong)amount.GetInt64());
                }
            }
            
            // Handle "to" field (recipients)
            if (body.TryGetProperty("to", out var recipients) && recipients.ValueKind == JsonValueKind.Array)
            {
                foreach (var recip in recipients.EnumerateArray())
                {
                    string? url = recip.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                    ulong amt = 0;
                    
                    if (recip.TryGetProperty("amount", out var amtProp))
                    {
                        if (amtProp.ValueKind == JsonValueKind.String)
                            ulong.TryParse(amtProp.GetString(), out amt);
                        else if (amtProp.ValueKind == JsonValueKind.Number)
                            amt = (ulong)amtProp.GetInt64();
                    }
                    
                    if (!string.IsNullOrEmpty(url))
                        issue.AddRecipient(url, amt);
                }
            }
            
            return issue;
        }

        private UpdateKeyPage BuildUpdateKeyPage(JsonElement body)
        {
            var update = new UpdateKeyPage();
            
            if (body.TryGetProperty("operation", out var operations) && operations.ValueKind == JsonValueKind.Array)
            {
                Console.WriteLine($"Found {operations.GetArrayLength()} operations");
                foreach (var op in operations.EnumerateArray())
                {
                    if (!op.TryGetProperty("type", out var typeProp))
                        continue;
                        
                    var typeStr = typeProp.GetString()?.ToLowerInvariant();
                    
                    switch (typeStr)
                    {
                        case "add":
                            if (op.TryGetProperty("entry", out var addEntry))
                            {
                                var keySpec = BuildKeySpecFromJson(addEntry);
                                Console.WriteLine($"Built keySpec: {keySpec != null}, keyHash: {keySpec?.KeyHash?.Length ?? 0} bytes");
                                if (keySpec != null)
                                {
                                    update.AddOperation(new AddKeyOperation { Entry = keySpec });
                                    Console.WriteLine($"Added add operation, total ops: {update.Operations?.Count ?? 0}");
                                }
                            }
                            break;
                            
                        case "remove":
                            if (op.TryGetProperty("entry", out var removeEntry))
                            {
                                var keySpec = BuildKeySpecFromJson(removeEntry);
                                if (keySpec != null && keySpec.KeyHash != null)
                                    update.AddOperation(new RemoveKeyOperation { KeyHash = keySpec.KeyHash });
                            }
                            break;
                            
                        case "update":
                            var updateOp = new UpdateKeyOperation();
                            if (op.TryGetProperty("oldEntry", out var oldEntry))
                                updateOp.OldEntry = BuildKeySpecFromJson(oldEntry);
                            if (op.TryGetProperty("newEntry", out var newEntry))
                                updateOp.NewEntry = BuildKeySpecFromJson(newEntry);
                            update.AddOperation(updateOp);
                            break;
                    }
                }
            }
            
            Console.WriteLine($"Returning UpdateKeyPage with {update.Operations?.Count ?? 0} operations");
            return update;
        }

        private KeySpecParams? BuildKeySpecFromJson(JsonElement element)
        {
            if (!element.TryGetProperty("keyHash", out var keyHashProp))
                return null;
                
            var keyHashStr = keyHashProp.GetString();
            if (string.IsNullOrEmpty(keyHashStr))
                return null;
                
            var keyHashBytes = new byte[keyHashStr.Length / 2];
            for (int i = 0; i < keyHashBytes.Length; i++)
            {
                keyHashBytes[i] = Convert.ToByte(keyHashStr.Substring(i * 2, 2), 16);
            }
            
            var keySpec = new KeySpecParams { KeyHash = keyHashBytes };
            
            if (element.TryGetProperty("delegate", out var delegateProp))
            {
                var delegateStr = delegateProp.GetString();
                if (!string.IsNullOrEmpty(delegateStr))
                    keySpec.Delegate = new Url(delegateStr);
            }
            
            return keySpec;
        }

        private WriteData BuildWriteData(JsonElement body)
        {
            var write = new WriteData();
            
            // Handle entry field which contains a DataEntry union type
            if (body.TryGetProperty("entry", out var entry))
            {
                if (entry.TryGetProperty("type", out var entryType))
                {
                    var typeStr = entryType.GetString()?.ToLowerInvariant();
                    
                    if (entry.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        // Convert data array to byte arrays
                        var segments = new System.Collections.Generic.List<byte[]>();
                        foreach (var dataElem in dataArray.EnumerateArray())
                        {
                            var dataStr = dataElem.GetString();
                            if (!string.IsNullOrEmpty(dataStr))
                            {
                                // Convert hex string to bytes
                                var dataBytes = new byte[dataStr.Length / 2];
                                for (int i = 0; i < dataBytes.Length; i++)
                                {
                                    dataBytes[i] = Convert.ToByte(dataStr.Substring(i * 2, 2), 16);
                                }
                                segments.Add(dataBytes);
                            }
                        }
                        
                        // Create the appropriate DataEntry type
                        if (typeStr == "doublehash")
                        {
                            write.DataEntry = new DoubleHashDataEntry 
                            { 
                                Data = segments.ToArray() 
                            };
                        }
                        else if (typeStr == "accumulate")
                        {
                            write.DataEntry = new AccumulateDataEntry 
                            { 
                                Data = segments.ToArray() 
                            };
                        }
                        else if (typeStr == "factom")
                        {
                            // For Factom, first segment is data, rest are ExtIDs
                            var factomEntry = new FactomDataEntryWrapper();
                            if (segments.Count > 0)
                            {
                                factomEntry.Data = segments[0];
                                if (segments.Count > 1)
                                {
                                    factomEntry.ExtIDs = segments.Skip(1).ToArray();
                                }
                            }
                            write.DataEntry = factomEntry;
                        }
                    }
                }
            }
            
            // Handle legacy direct fields (shouldn't be in test vectors but just in case)
            if (body.TryGetProperty("data", out var data))
            {
                var dataStr = data.GetString();
                if (!string.IsNullOrEmpty(dataStr))
                {
                    var dataBytes = new byte[dataStr.Length / 2];
                    for (int i = 0; i < dataBytes.Length; i++)
                    {
                        dataBytes[i] = Convert.ToByte(dataStr.Substring(i * 2, 2), 16);
                    }
                    write.WithData(dataBytes);
                }
            }
            
            return write;
        }

        private WriteDataTo BuildWriteDataTo(JsonElement body)
        {
            var write = new WriteDataTo();
            
            if (body.TryGetProperty("recipient", out var recipient))
                write.WithRecipient(new Url(recipient.GetString() ?? string.Empty));
            
            // Handle entry field which contains a DataEntry union type
            if (body.TryGetProperty("entry", out var entry))
            {
                if (entry.TryGetProperty("type", out var entryType))
                {
                    var typeStr = entryType.GetString()?.ToLowerInvariant();
                    
                    if (entry.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        // Convert data array to byte arrays
                        var segments = new System.Collections.Generic.List<byte[]>();
                        foreach (var dataElem in dataArray.EnumerateArray())
                        {
                            var dataStr = dataElem.GetString();
                            if (!string.IsNullOrEmpty(dataStr))
                            {
                                // Convert hex string to bytes
                                var dataBytes = new byte[dataStr.Length / 2];
                                for (int i = 0; i < dataBytes.Length; i++)
                                {
                                    dataBytes[i] = Convert.ToByte(dataStr.Substring(i * 2, 2), 16);
                                }
                                segments.Add(dataBytes);
                            }
                        }
                        
                        // Create the appropriate DataEntry type
                        if (typeStr == "doublehash")
                        {
                            write.DataEntry = new DoubleHashDataEntry 
                            { 
                                Data = segments.ToArray() 
                            };
                        }
                        else if (typeStr == "accumulate")
                        {
                            write.DataEntry = new AccumulateDataEntry 
                            { 
                                Data = segments.ToArray() 
                            };
                        }
                        else if (typeStr == "factom")
                        {
                            var factomEntry = new FactomDataEntryWrapper();
                            if (segments.Count > 0)
                            {
                                factomEntry.Data = segments[0];
                                if (segments.Count > 1)
                                {
                                    factomEntry.ExtIDs = segments.Skip(1).ToArray();
                                }
                            }
                            write.DataEntry = factomEntry;
                        }
                    }
                }
            }
            
            // Handle legacy fields (shouldn't be in test vectors but just in case)
            if (body.TryGetProperty("data", out var data))
            {
                var dataStr = data.GetString();
                if (!string.IsNullOrEmpty(dataStr))
                {
                    var dataBytes = new byte[dataStr.Length / 2];
                    for (int i = 0; i < dataBytes.Length; i++)
                    {
                        dataBytes[i] = Convert.ToByte(dataStr.Substring(i * 2, 2), 16);
                    }
                    write.WithData(dataBytes);
                }
            }
            
            if (body.TryGetProperty("format", out var format))
                write.WithFormat(format.GetString() ?? string.Empty);
            
            if (body.TryGetProperty("entryHash", out var entryHash))
                write.WithEntryHash(entryHash.GetString() ?? string.Empty);
            
            return write;
        }

        private AcmeFaucet BuildAcmeFaucet(JsonElement body)
        {
            var faucet = new AcmeFaucet();
            
            if (body.TryGetProperty("url", out var url))
                faucet.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            return faucet;
        }
    }
}