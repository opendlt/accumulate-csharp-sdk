using System;
using Xunit;
using Xunit.Abstractions;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class MultipleRecipientsTest
    {
        private readonly ITestOutputHelper _output;

        public MultipleRecipientsTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestSendTokensWithMultipleRecipients()
        {
            // Create SendTokens with multiple recipients
            var sendTokens = new SendTokens();
            sendTokens.AddRecipient("acc://alice.acme/ACME", 100);
            sendTokens.AddRecipient("acc://bob.acme/ACME", 200);
            sendTokens.AddRecipient("acc://charlie.acme/ACME", 300);
            
            // Marshal to binary
            var bytes = sendTokens.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"SendTokens with 3 recipients marshalled:");
            _output.WriteLine($"Hex: {hex}");
            _output.WriteLine($"Length: {bytes.Length} bytes");
            
            // Verify structure
            // Should start with field 1 (type): 01 03
            Assert.StartsWith("0103", hex);
            
            // Should have 3 occurrences of field 4 (one for each recipient)
            int field4Count = 0;
            for (int i = 0; i < hex.Length - 1; i += 2)
            {
                if (hex.Substring(i, 2) == "04")
                {
                    field4Count++;
                }
            }
            _output.WriteLine($"Found {field4Count} field 4 entries (recipients)");
            Assert.Equal(3, field4Count);
            
            // Verify each recipient's amount is present
            Assert.Contains("020164", hex); // 100 = 0x64
            Assert.Contains("0201c8", hex); // 200 = 0xc8  
            
            // 300 = 0x012c (two bytes) in big-endian
            Assert.Contains("0202012c", hex); // 300 = 0x012c in big-endian
            
            _output.WriteLine("✓ All recipients properly marshalled!");
        }

        [Fact]
        public void TestTransactionHashWithMultipleRecipients()
        {
            // Create a complete transaction with multiple recipients
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://sender.acme/ACME")
                    .WithInitiator("84e032fba8a5456f631c822a2b2466c18b3fa7804330ab87088ed6e30d690505"),
                Body = new SendTokens()
                    .AddRecipient("acc://alice.acme/ACME", 100)
                    .AddRecipient("acc://bob.acme/ACME", 200)
                    .AddRecipient("acc://charlie.acme/ACME", 300)
            };

            // Compute hash
            byte[] hash = TransactionHasher.ComputeRawHash(transaction);
            string hashHex = new string(Hex.EncodeHex(hash));
            
            _output.WriteLine($"Transaction hash with multiple recipients: {hashHex}");
            
            // Verify hash is computed (should be 32 bytes)
            Assert.Equal(32, hash.Length);
            
            // Verify it's deterministic
            byte[] hash2 = TransactionHasher.ComputeRawHash(transaction);
            Assert.Equal(hash, hash2);
            
            _output.WriteLine("✓ Hash computation works with multiple recipients!");
        }

        [Fact]
        public void TestSendTokensBuilderWithMultipleRecipients()
        {
            // Test using the builder pattern
            var sendTokens = new SendTokens()
                .AddRecipient("acc://alice.acme/ACME", 1000)
                .AddRecipient("acc://bob.acme/ACME", 2000)
                .AddRecipient("acc://charlie.acme/ACME", 3000)
                .AddRecipient("acc://david.acme/ACME", 4000)
                .AddRecipient("acc://eve.acme/ACME", 5000);
            
            Assert.Equal(5, sendTokens.Recipients.Count);
            
            var bytes = sendTokens.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"SendTokens with 5 recipients:");
            _output.WriteLine($"Length: {bytes.Length} bytes");
            
            // Count field 4 occurrences
            int field4Count = 0;
            for (int i = 0; i < hex.Length - 1; i += 2)
            {
                if (hex.Substring(i, 2) == "04")
                {
                    field4Count++;
                }
            }
            
            Assert.Equal(5, field4Count);
            _output.WriteLine($"✓ Successfully marshalled {field4Count} recipients!");
        }

        [Fact]
        public void TestLargeAmountMarshalling()
        {
            // Test with very large amounts to ensure BigInt handling works
            var sendTokens = new SendTokens();
            
            // Add recipient with large amount (1 million)
            sendTokens.AddRecipient("acc://alice.acme/ACME", 1_000_000);
            
            // Add recipient with very large amount (10 billion)
            sendTokens.AddRecipient("acc://bob.acme/ACME", 10_000_000_000);
            
            var bytes = sendTokens.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"SendTokens with large amounts:");
            _output.WriteLine($"Hex: {hex}");
            
            // 1,000,000 = 0x0F4240 (3 bytes) in big-endian
            Assert.Contains("02030f4240", hex); // big-endian: 0F 42 40
            
            // 10,000,000,000 = 0x2540BE400 (5 bytes) in big-endian
            Assert.Contains("020502540be400", hex); // big-endian: 02 54 0B E4 00
            
            _output.WriteLine("✓ Large amounts properly marshalled as BigInt!");
        }

        [Fact]
        public void TestEmptyRecipientsListMarshalling()
        {
            // Test edge case: SendTokens with no recipients
            var sendTokens = new SendTokens();
            
            var bytes = sendTokens.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"SendTokens with no recipients: {hex}");
            
            // Should only have field 1 (type)
            Assert.Equal("0103", hex);
            
            _output.WriteLine("✓ Empty recipients list handled correctly!");
        }
    }
}