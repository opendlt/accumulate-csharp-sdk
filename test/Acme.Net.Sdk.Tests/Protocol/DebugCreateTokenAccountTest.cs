using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class DebugCreateTokenAccountTest
    {
        private readonly ITestOutputHelper _output;

        public DebugCreateTokenAccountTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DebugCreateTokenAccountHash()
        {
            // Get test vector data
            var testVectors = TestVectors.Vectors;
            var group = testVectors.Transactions.FirstOrDefault(g => g.Name == "CreateTokenAccount");
            Assert.NotNull(group);
            
            var testCase = group.Cases[0];
            var txData = testCase.Json.Transaction[0];
            
            // Create the transaction from test vector
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url(txData.Header.Principal))
            };
            
            if (!string.IsNullOrEmpty(txData.Header.Initiator))
            {
                transaction.Header = transaction.Header.WithInitiator(txData.Header.Initiator);
            }

            // Build CreateTokenAccount
            var createTokenAccount = new CreateTokenAccount();
            
            if (txData.Body.TryGetProperty("url", out var url))
                createTokenAccount.WithUrl(new Url(url.GetString() ?? string.Empty));
            
            if (txData.Body.TryGetProperty("tokenUrl", out var tokenUrl))
            {
                var tokenUrlString = tokenUrl.GetString() ?? string.Empty;
                _output.WriteLine($"Token URL from test vector: {tokenUrlString}");
                createTokenAccount.WithTokenUrl(new Url(tokenUrlString));
            }

            if (txData.Body.TryGetProperty("authorities", out var authorities) && 
                authorities.ValueKind == System.Text.Json.JsonValueKind.Array)
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

            // Get the raw bytes
            byte[] headerBytes = transaction.Header.MarshalBinary();
            byte[] bodyBytes = ((ITransactionBody)transaction.Body).MarshalBinary();
            
            _output.WriteLine($"Header bytes: {new string(Hex.EncodeHex(headerBytes))}");
            _output.WriteLine($"Body bytes: {new string(Hex.EncodeHex(bodyBytes))}");
            
            // Compute hashes
            byte[] headerHash = HashUtils.Sha256(headerBytes);
            byte[] bodyHash = HashUtils.Sha256(bodyBytes);
            byte[] combined = new byte[64];
            Array.Copy(headerHash, 0, combined, 0, 32);
            Array.Copy(bodyHash, 0, combined, 32, 32);
            byte[] finalHash = HashUtils.Sha256(combined);
            
            _output.WriteLine($"Header hash: {new string(Hex.EncodeHex(headerHash))}");
            _output.WriteLine($"Body hash: {new string(Hex.EncodeHex(bodyHash))}");
            _output.WriteLine($"Final hash: {new string(Hex.EncodeHex(finalHash))}");
            
            // Expected from test vector
            string expectedHash = testCase.Json.Signatures[0].TransactionHash;
            _output.WriteLine($"Expected hash: {expectedHash}");
            
            // Now let's look at what the test vector body looks like
            var testBinary = Convert.FromBase64String(testCase.Binary);
            _output.WriteLine($"Test vector full binary ({testBinary.Length} bytes): {new string(Hex.EncodeHex(testBinary))}");
            
            // Try to find the body portion in the test vector
            // The body typically starts after the header
            // Let's search for our body pattern
            var ourBodyHex = new string(Hex.EncodeHex(bodyBytes));
            _output.WriteLine($"Looking for our body: {ourBodyHex}");
            
            // Extract what looks like the body from test vector
            // Skip envelope metadata and header to find body
            // The body usually starts with field 2 in the envelope
            _output.WriteLine($"Match found: {new string(Hex.EncodeHex(testBinary)).Contains(ourBodyHex.Substring(0, Math.Min(20, ourBodyHex.Length)))}");
        }
    }
}