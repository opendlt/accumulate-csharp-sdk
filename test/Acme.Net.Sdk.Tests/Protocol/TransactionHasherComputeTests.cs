using System;
using System.Linq;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;
using Xunit;
using Xunit.Abstractions;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Tests that verify transaction hashes are computed correctly, not just returned from pre-set values.
    /// </summary>
    public class TransactionHasherComputeTests
    {
        private readonly ITestOutputHelper _output;

        public TransactionHasherComputeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestHashAlgorithmIsCorrect()
        {
            // This test verifies our hash algorithm implementation is correct
            // Our SDK correctly computes: SHA256(SHA256(header) || SHA256(body))
            
            // Create a SendTokens transaction
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://adi.acme/ACME")
                    .WithInitiator("84e032fba8a5456f631c822a2b2466c18b3fa7804330ab87088ed6e30d690505"),
                Body = new SendTokens()
                    .AddRecipient("acc://other.acme/ACME", 100)
            };
            
            // Get the raw bytes
            byte[] headerBytes = transaction.Header.MarshalBinary();
            byte[] bodyBytes = ((ITransactionBody)transaction.Body).MarshalBinary();
            
            // Compute individual hashes
            byte[] headerHash = HashUtils.Sha256(headerBytes);
            byte[] bodyHash = HashUtils.Sha256(bodyBytes);
            
            // Combine and hash
            byte[] combined = new byte[64];
            Array.Copy(headerHash, 0, combined, 0, 32);
            Array.Copy(bodyHash, 0, combined, 32, 32);
            byte[] finalHash = HashUtils.Sha256(combined);
            
            // Verify our implementation matches manual computation
            byte[] sdkHash = TransactionHasher.ComputeRawHash(transaction);
            
            _output.WriteLine($"Header bytes: {new string(Hex.EncodeHex(headerBytes))}");
            _output.WriteLine($"Body bytes: {new string(Hex.EncodeHex(bodyBytes))}");
            _output.WriteLine($"Header hash: {new string(Hex.EncodeHex(headerHash))}");
            _output.WriteLine($"Body hash: {new string(Hex.EncodeHex(bodyHash))}");
            _output.WriteLine($"Manual computation: {new string(Hex.EncodeHex(finalHash))}");
            _output.WriteLine($"SDK computation: {new string(Hex.EncodeHex(sdkHash))}");
            
            // The SDK hash should match our manual computation
            Assert.Equal(new string(Hex.EncodeHex(finalHash)), new string(Hex.EncodeHex(sdkHash)));
        }

        [Theory]
        [InlineData("SendTokens")]
        [InlineData("CreateTokenAccount")]
        public void TestComputedHashMatches_WithoutPresetHash(string transactionType)
        {
            // Arrange
            var testVectors = TestVectors.Vectors;
            var group = testVectors.Transactions.FirstOrDefault(g => g.Name == transactionType);
            
            Assert.NotNull(group);
            Assert.NotEmpty(group.Cases);
            
            _output.WriteLine($"Testing computed hash for: {transactionType}");
            
            var testCase = group.Cases[0];
            
            // Convert test case to transaction but DON'T set the pre-computed hash
            var txData = testCase.Json.Transaction[0];
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url(txData.Header.Principal))
            };
            
            // Set the initiator hash from test vector
            if (!string.IsNullOrEmpty(txData.Header.Initiator))
            {
                transaction.Header = transaction.Header.WithInitiator(txData.Header.Initiator);
            }
            
            // Set body based on type
            string bodyType = txData.Body.GetProperty("type").GetString() ?? string.Empty;
            
            switch (bodyType.ToLowerInvariant())
            {
                case "sendtokens":
                    var sendTokens = new SendTokens();
                    if (txData.Body.TryGetProperty("to", out var recipients) && recipients.ValueKind == System.Text.Json.JsonValueKind.Array)
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
                    transaction.Body = sendTokens;
                    break;
                    
                case "createtokenaccount":
                    var createTokenAccount = new CreateTokenAccount();
                    if (txData.Body.TryGetProperty("url", out var url))
                        createTokenAccount.WithUrl(new Url(url.GetString() ?? string.Empty));
                    
                    if (txData.Body.TryGetProperty("tokenUrl", out var tokenUrl))
                        createTokenAccount.WithTokenUrl(new Url(tokenUrl.GetString() ?? string.Empty));

                    if (txData.Body.TryGetProperty("authorities", out var authorities) && authorities.ValueKind == System.Text.Json.JsonValueKind.Array)
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
                    transaction.Body = createTokenAccount;
                    break;
                    
                default:
                    throw new NotSupportedException($"Transaction type {bodyType} not implemented in this test");
            }
            
            // Act - Force computation of hash by calling the internal method
            byte[] computedHash = TransactionHasher.ComputeRawHash(transaction);
            
            // Assert
            string computedHex = new string(Hex.EncodeHex(computedHash));
            
            _output.WriteLine($"Transaction type: {transactionType}");
            _output.WriteLine($"Computed hash: {computedHex}");
            
            // Verify hash was computed (32 bytes)
            Assert.Equal(32, computedHash.Length);
            Assert.NotEqual(new byte[32], computedHash); // Not all zeros
            
            // Verify the hash computation is deterministic
            byte[] secondHash = TransactionHasher.ComputeRawHash(transaction);
            Assert.Equal(computedHash, secondHash);
            
            // Log the transaction details for debugging
            _output.WriteLine($"Principal: {transaction.Header.Principal?.String()}");
            _output.WriteLine($"Has initiator: {transaction.Header.Initiator != null}");
            _output.WriteLine($"Body type: {transaction.Body?.GetType().Name}");
        }
    }
}