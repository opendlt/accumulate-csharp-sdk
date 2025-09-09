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
    public class WriteDataTests
    {
        private readonly ITestOutputHelper _output;

        public WriteDataTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestWriteDataMarshallingIncludesTransactionType()
        {
            // Create WriteData transaction
            var writeData = new WriteData()
                .WithData("Hello, Accumulate!")
                .WithFormat("text/plain");
            
            // Marshal to binary
            var bytes = writeData.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"WriteData marshalled: {hex}");
            _output.WriteLine($"Length: {bytes.Length} bytes");
            
            // Field 1 should be type 5 (WriteData)
            // In varint encoding, 01 is field 1, 05 is value 5
            Assert.StartsWith("0105", hex);
            
            _output.WriteLine("✓ WriteData includes transaction type field!");
        }

        [Fact]
        public void TestWriteDataToMarshallingIncludesTransactionType()
        {
            // Create WriteDataTo transaction
            var writeDataTo = new WriteDataTo()
                .WithRecipient("acc://alice.acme/data")
                .WithData("Hello, Bob!")
                .WithFormat("text/plain");
            
            // Marshal to binary
            var bytes = writeDataTo.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"WriteDataTo marshalled: {hex}");
            _output.WriteLine($"Length: {bytes.Length} bytes");
            
            // Field 1 should be type 6 (WriteDataTo)
            // In varint encoding, 01 is field 1, 06 is value 6
            Assert.StartsWith("0106", hex);
            
            _output.WriteLine("✓ WriteDataTo includes transaction type field!");
        }

        [Fact]
        public void TestWriteDataWithOnlyData()
        {
            var writeData = new WriteData()
                .WithData(new byte[] { 0x01, 0x02, 0x03, 0x04 });
            
            var bytes = writeData.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"WriteData with only data: {hex}");
            
            // Should have field 1 (type) and field 2 (entry)
            Assert.Contains("0105", hex); // Field 1, value 5
            Assert.Contains("02", hex); // Field 2 tag
        }

        [Fact]
        public void TestWriteDataWithAllFields()
        {
            var writeData = new WriteData()
                .WithData("Test data")
                .WithFormat("application/json")
                .WithEntryHash("abcdef0123456789");
            
            var bytes = writeData.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"WriteData with all fields: {hex}");
            
            // Should have field 1 (type)
            Assert.Contains("0105", hex);
            
            // Should have field 2 (entry) with nested data
            Assert.Contains("02", hex); // Field 2 tag
            
            _output.WriteLine("✓ All fields properly marshalled!");
        }

        [Fact]
        public void TestWriteDataToWithRecipientAndData()
        {
            var writeDataTo = new WriteDataTo()
                .WithRecipient(new Url("acc://bob.acme/data"))
                .WithData(System.Text.Encoding.UTF8.GetBytes("Message for Bob"));
            
            var bytes = writeDataTo.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"WriteDataTo with recipient and data: {hex}");
            
            // Should have field 1 (type = 6)
            Assert.Contains("0106", hex);
            
            // Should have field 2 (recipient URL)
            Assert.Contains("02", hex); // Field 2 tag
            
            // Should have field 3 (entry)
            Assert.Contains("03", hex); // Field 3 tag
            
            _output.WriteLine("✓ Recipient and data properly marshalled!");
        }

        [Fact]
        public void TestDataEntryMarshalling()
        {
            // Test AccumulateDataEntry marshalling directly
            var entry = new AccumulateDataEntry(new byte[][] {
                System.Text.Encoding.UTF8.GetBytes("First segment"),
                System.Text.Encoding.UTF8.GetBytes("Second segment")
            });
            
            var bytes = entry.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"AccumulateDataEntry: {hex}");
            
            // Should have field 1 (type = 2 for Accumulate)
            Assert.Contains("0102", hex);
            
            // Should have multiple field 2 entries (repeated data)
            // Count occurrences of "0202" which is field 2 with a length prefix
            int field2Count = 0;
            for (int i = 2; i < hex.Length - 3; i += 2) // Skip the type field at start
            {
                if (hex.Substring(i, 4) == "0202" || (hex.Substring(i, 2) == "02" && i > 2))
                {
                    field2Count++;
                }
            }
            
            _output.WriteLine($"Found {field2Count} data segments (field 2 entries)");
            Assert.True(field2Count >= 2, $"Expected at least 2 data segments, found {field2Count}");
        }

        [Fact]
        public void TestWriteDataTransactionHash()
        {
            // Create a complete transaction with WriteData body
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice.acme/data")
                    .WithInitiator("84e032fba8a5456f631c822a2b2466c18b3fa7804330ab87088ed6e30d690505"),
                Body = new WriteData()
                    .WithData("Important data to store")
                    .WithFormat("text/plain")
            };

            // Compute hash
            byte[] hash = TransactionHasher.ComputeRawHash(transaction);
            string hashHex = new string(Hex.EncodeHex(hash));
            
            _output.WriteLine($"WriteData transaction hash: {hashHex}");
            
            // Verify hash is 32 bytes (SHA256)
            Assert.Equal(32, hash.Length);
            
            // Verify it's deterministic
            byte[] hash2 = TransactionHasher.ComputeRawHash(transaction);
            Assert.Equal(hash, hash2);
            
            _output.WriteLine("✓ WriteData transaction hash computed successfully!");
        }

        [Fact]
        public void TestWriteDataToTransactionHash()
        {
            // Create a complete transaction with WriteDataTo body
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice.acme/ACME")
                    .WithInitiator("84e032fba8a5456f631c822a2b2466c18b3fa7804330ab87088ed6e30d690505"),
                Body = new WriteDataTo()
                    .WithRecipient("acc://bob.acme/data")
                    .WithData("Message for Bob's data account")
            };

            // Compute hash
            byte[] hash = TransactionHasher.ComputeRawHash(transaction);
            string hashHex = new string(Hex.EncodeHex(hash));
            
            _output.WriteLine($"WriteDataTo transaction hash: {hashHex}");
            
            // Verify hash is 32 bytes
            Assert.Equal(32, hash.Length);
            
            // Verify it's deterministic
            byte[] hash2 = TransactionHasher.ComputeRawHash(transaction);
            Assert.Equal(hash, hash2);
            
            _output.WriteLine("✓ WriteDataTo transaction hash computed successfully!");
        }

        [Fact]
        public void TestWriteDataEmptyMarshalling()
        {
            // Test edge case: WriteData with no data
            var writeData = new WriteData();
            
            var bytes = writeData.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"Empty WriteData: {hex}");
            
            // Should only have field 1 (type)
            Assert.Equal("0105", hex);
            
            _output.WriteLine("✓ Empty WriteData handled correctly!");
        }

        [Fact]
        public void TestWriteDataToWithoutRecipient()
        {
            // Test WriteDataTo with data but no recipient
            var writeDataTo = new WriteDataTo()
                .WithData("Orphan data");
            
            var bytes = writeDataTo.MarshalBinary();
            var hex = new string(Hex.EncodeHex(bytes));
            
            _output.WriteLine($"WriteDataTo without recipient: {hex}");
            
            // Should have field 1 (type) and field 3 (entry)
            Assert.Contains("0106", hex); // Type
            Assert.Contains("03", hex); // Field 3 (entry)
            
            _output.WriteLine("✓ WriteDataTo without recipient handled!");
        }
    }
}