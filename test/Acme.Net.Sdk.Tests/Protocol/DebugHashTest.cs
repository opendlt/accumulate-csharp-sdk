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
    public class DebugHashTest
    {
        private readonly ITestOutputHelper _output;

        public DebugHashTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DebugSendTokensHash()
        {
            // Create the exact transaction from test vector
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
            string expectedHash = "a76432672cd7834eb26873a33d3e00a350c5a9a3aff9b7cc50637db4931e4b47";
            _output.WriteLine($"Expected hash: {expectedHash}");
            
            // Now let's look at what the test vector body looks like
            // From the test vector, the body portion is: 021e0103041a01156163633a2f2f6f746865722e61636d652f41434d45020164
            string testVectorBody = "021e0103041a01156163633a2f2f6f746865722e61636d652f41434d45020164";
            byte[] testVectorBodyBytes = Hex.DecodeHex(testVectorBody.ToCharArray());
            _output.WriteLine($"Test vector body: {testVectorBody}");
            
            // The test vector body is wrapped in field 2, let's extract the inner content
            // Skip the first 2 bytes (02 1e) which is field 2 with length 30
            byte[] innerBody = new byte[testVectorBodyBytes.Length - 2];
            Array.Copy(testVectorBodyBytes, 2, innerBody, 0, innerBody.Length);
            _output.WriteLine($"Inner body: {new string(Hex.EncodeHex(innerBody))}");
            
            // Compare with what we produce
            _output.WriteLine($"Our body matches inner? {new string(Hex.EncodeHex(bodyBytes)) == new string(Hex.EncodeHex(innerBody))}");
        }
    }
}