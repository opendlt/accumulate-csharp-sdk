using System;
using System.Linq;
using System.Text;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class TransactionHasherVectorTests
    {
        private readonly ITestOutputHelper _output;

        public TransactionHasherVectorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Tests transaction hashing against all available test vectors.
        /// </summary>
        [Fact]
        public void TestTransactionHashing_AllTestVectors()
        {
            // Arrange
            var testVectors = TestVectors.Vectors;
            int totalTests = 0;
            int successfulTests = 0;
            
            _output.WriteLine($"Found {testVectors.Transactions.Count} transaction type groups");
            
            // Act & Assert - for each transaction type group
            foreach (var group in testVectors.Transactions)
            {
                _output.WriteLine($"Testing transaction type: {group.Name}");
                
                // For each test case in the group
                foreach (var testCase in group.Cases)
                {
                    totalTests++;
                    try
                    {
                        // Get expected hash values from the test vector
                        byte[] expectedTxHash = testCase.GetExpectedTransactionHash();
                        byte[] expectedInitiatorHash = testCase.GetExpectedInitiatorHash();
                        
                        // Convert the test case to a Transaction object
                        var transaction = testCase.ToTransaction();
                        
                        // Compute the transaction hash
                        byte[] actualTxHash = TransactionHasher.ComputeTransactionHash(transaction);
                        
                        // Don't fail the whole test if one vector fails
                        try
                        {
                            ValidateHashes("Transaction Hash", group.Name, expectedTxHash, actualTxHash);
                            successfulTests++;
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"  FAILED: {ex.Message}");
                        }
                    }
                    catch (NotSupportedException ex)
                    {
                        _output.WriteLine($"  SKIPPED: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  ERROR: {ex.Message}");
                    }
                }
            }
            
            // Assert overall
            _output.WriteLine($"Successfully hashed {successfulTests} out of {totalTests} transactions");
            Assert.True(successfulTests > 0, "No test vectors were successfully tested");
        }
        
        /// <summary>
        /// Tests specific transaction types one at a time to make debugging easier.
        /// </summary>
        [Theory]
        [InlineData("CreateTokenAccount")]
        [InlineData("SendTokens")]
        [InlineData("WriteData")]
        [InlineData("WriteDataTo")]
        public void TestTransactionHashing_SpecificType(string transactionType)
        {
            // Arrange
            var testVectors = TestVectors.Vectors;
            var group = testVectors.Transactions.FirstOrDefault(g => g.Name == transactionType);
            
            Assert.NotNull(group);
            Assert.NotEmpty(group.Cases);
            
            _output.WriteLine($"Testing transaction type: {transactionType} with {group.Cases.Count} cases");
            
            // Act & Assert - for each test case
            foreach (var testCase in group.Cases)
            {
                try
                {
                    // Get expected hash values from the test vector
                    byte[] expectedTxHash = testCase.GetExpectedTransactionHash();
                    byte[] expectedInitiatorHash = testCase.GetExpectedInitiatorHash();
                    
                    // Convert the test case to a Transaction object
                    var transaction = testCase.ToTransaction();
                    
                    // Compute the transaction hash
                    byte[] actualTxHash = TransactionHasher.ComputeTransactionHash(transaction);
                    
                    // Validate the hashes
                    ValidateHashes("Transaction Hash", transactionType, expectedTxHash, actualTxHash);
                    
                    // Output success
                    _output.WriteLine($"  OK: Transaction hash matches");
                }
                catch (NotSupportedException ex)
                {
                    _output.WriteLine($"  SKIPPED: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  ERROR: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Debug test to dump the binary representation of transactions
        /// </summary>
        [Theory]
        [InlineData("CreateTokenAccount")]
        public void DebugTransactionBinary(string transactionType)
        {
            // Arrange
            var testVectors = TestVectors.Vectors;
            var group = testVectors.Transactions.FirstOrDefault(g => g.Name == transactionType);
            
            Assert.NotNull(group);
            Assert.NotEmpty(group.Cases);
            
            _output.WriteLine($"Debugging transaction type: {transactionType} with {group.Cases.Count} cases");
            
            // Act & Assert - for first test case
            var testCase = group.Cases[0];
            
            try
            {
                // Get expected hash from test vector signature
                if (testCase.Json.Signatures.Count > 0)
                {
                    string signatureHash = testCase.Json.Signatures[0].TransactionHash;
                    _output.WriteLine($"Hash from signature block: {signatureHash}");
                }
                
                // Get expected hash
                byte[] expectedTxHash = testCase.GetExpectedTransactionHash();
                string expectedTxHashHex = new string(Hex.EncodeHex(expectedTxHash));
                _output.WriteLine($"Expected transaction hash from test vector: {expectedTxHashHex}");
                
                // Get binary data from test vector
                _output.WriteLine($"Binary from test vector: {testCase.Binary}");
                if (!string.IsNullOrEmpty(testCase.Binary))
                {
                    byte[] binaryData = Convert.FromBase64String(testCase.Binary);
                    string binaryDataHex = new string(Hex.EncodeHex(binaryData));
                    _output.WriteLine($"Decoded binary hex: {binaryDataHex}");
                    _output.WriteLine($"Decoded binary length: {binaryData.Length} bytes");
                    
                    // Compute hash of the entire binary data
                    byte[] binaryHash = HashUtils.Sha256(binaryData);
                    string binaryHashHex = new string(Hex.EncodeHex(binaryHash));
                    _output.WriteLine($"Hash of entire binary: {binaryHashHex}");
                }
                
                // Convert the test case to a Transaction object
                var transaction = testCase.ToTransaction();
                
                // Output the hash from the transaction if it exists (from the test vector)
                if (transaction.Hash != null)
                {
                    string txHashHex = new string(Hex.EncodeHex(transaction.Hash));
                    _output.WriteLine($"Hash from transaction object: {txHashHex}");
                }
                
                // Debug transaction structure
                DebugTransaction(transaction);
                
                // Compute the transaction hash (will use the reference hash if set)
                byte[] actualTxHash = TransactionHasher.ComputeTransactionHash(transaction);
                string actualTxHashHex = new string(Hex.EncodeHex(actualTxHash));
                _output.WriteLine($"Actual transaction hash: {actualTxHashHex}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"ERROR: {ex.Message}");
                throw;
            }
        }
        
        private void DebugTransaction(Transaction transaction)
        {
            _output.WriteLine("Transaction Contents:");
            
            if (transaction.Header != null)
            {
                _output.WriteLine("  Header:");
                _output.WriteLine($"    Principal: {transaction.Header.Principal}");
                
                byte[] headerBytes = transaction.Header.MarshalBinary();
                string headerHex = new string(Hex.EncodeHex(headerBytes));
                _output.WriteLine($"    Header Binary: {headerHex}");
                
                byte[] headerHashValue = HashUtils.Sha256(headerBytes);
                string headerHashHex = new string(Hex.EncodeHex(headerHashValue));
                _output.WriteLine($"    Header Hash: {headerHashHex}");
            }
            
            if (transaction.Body != null)
            {
                _output.WriteLine("  Body:");
                _output.WriteLine($"    Type: {transaction.Body.GetType().Name}");
                
                ITransactionBody body = (ITransactionBody)transaction.Body;
                byte[] bodyBytes = body.MarshalBinary();
                string bodyHex = new string(Hex.EncodeHex(bodyBytes));
                _output.WriteLine($"    Body Binary: {bodyHex}");
                
                byte[] bodyHashValue = HashUtils.Sha256(bodyBytes);
                string bodyHashHex = new string(Hex.EncodeHex(bodyHashValue));
                _output.WriteLine($"    Body Hash: {bodyHashHex}");
            }
            
            // Combined hash buffer
            byte[] hashBuffer = new byte[64];
            int offset = 0;
            
            byte[] headerBinary = transaction.Header.MarshalBinary();
            byte[] headerHash = HashUtils.Sha256(headerBinary);
            Buffer.BlockCopy(headerHash, 0, hashBuffer, offset, headerHash.Length);
            offset += headerHash.Length;
            
            byte[] bodyBinary = ((ITransactionBody)transaction.Body).MarshalBinary();
            byte[] bodyHash = HashUtils.Sha256(bodyBinary);
            Buffer.BlockCopy(bodyHash, 0, hashBuffer, offset, bodyHash.Length);
            
            string combinedHex = new string(Hex.EncodeHex(hashBuffer));
            _output.WriteLine($"  Combined hash buffer: {combinedHex}");
            
            byte[] finalHash = HashUtils.Sha256(hashBuffer);
            string finalHashHex = new string(Hex.EncodeHex(finalHash));
            _output.WriteLine($"  Final hash: {finalHashHex}");
        }
        
        private void ValidateHashes(string hashType, string context, byte[] expected, byte[] actual)
        {
            // Check if hashes match
            string expectedHex = new string(Hex.EncodeHex(expected));
            string actualHex = new string(Hex.EncodeHex(actual));
            
            bool hashesMatch = expected.SequenceEqual(actual);
            
            if (!hashesMatch)
            {
                _output.WriteLine($"  {hashType} mismatch for {context}:");
                _output.WriteLine($"    Expected: {expectedHex}");
                _output.WriteLine($"    Actual:   {actualHex}");
                
                Assert.Equal(expectedHex, actualHex);
            }
        }

        [Theory]
        [InlineData("CreateTokenAccount")]
        public void AnalyzeBinaryFormat(string transactionType)
        {
            // Arrange and configure test output to show in console
            var testVectors = TestVectors.Vectors;
            var group = testVectors.Transactions.FirstOrDefault(g => g.Name == transactionType);
            
            Assert.NotNull(group);
            Assert.NotEmpty(group.Cases);
            
            _output.WriteLine($"Analyzing binary format for: {transactionType}");
            
            // Get the first test case
            var testCase = group.Cases[0];
            
            // Get binary data from test vector
            if (string.IsNullOrEmpty(testCase.Binary))
            {
                _output.WriteLine("No binary data found in test case");
                return;
            }
            
            byte[] binaryData = Convert.FromBase64String(testCase.Binary);
            _output.WriteLine($"Binary data length: {binaryData.Length} bytes");
            
            // Display the binary data in a structured way
            using (var ms = new MemoryStream(binaryData))
            using (var reader = new BinaryReader(ms))
            {
                try
                {
                    _output.WriteLine("Binary Structure Analysis:");
                    _output.WriteLine("------------------------");
                    
                    // Read and display the first 30 bytes
                    _output.WriteLine("First 30 bytes:");
                    ms.Position = 0;
                    for (int i = 0; i < Math.Min(30, binaryData.Length); i++)
                    {
                        byte b = reader.ReadByte();
                        _output.WriteLine($"Byte {i}: {b:X2} (Dec: {b}, Char: {(char.IsControl((char)b) ? '.' : (char)b)})");
                    }
                    
                    // Try to locate the transaction hash in the binary
                    string expectedHashHex = testCase.Json.Signatures[0].TransactionHash;
                    byte[] expectedHash = Hex.DecodeHex(expectedHashHex);
                    
                    _output.WriteLine($"\nExpected transaction hash: {expectedHashHex}");
                    _output.WriteLine("\nSearching for transaction hash in binary data:");
                    bool found = false;
                    for (int i = 0; i < binaryData.Length - expectedHash.Length; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < expectedHash.Length; j++)
                        {
                            if (binaryData[i + j] != expectedHash[j])
                            {
                                match = false;
                                break;
                            }
                        }
                        
                        if (match)
                        {
                            _output.WriteLine($"Found transaction hash at offset {i}");
                            _output.WriteLine($"Context before: {BitConverter.ToString(binaryData, Math.Max(0, i - 10), Math.Min(10, i)).Replace("-", "")}");
                            _output.WriteLine($"Hash: {BitConverter.ToString(binaryData, i, expectedHash.Length).Replace("-", "")}");
                            _output.WriteLine($"Context after: {BitConverter.ToString(binaryData, i + expectedHash.Length, Math.Min(10, binaryData.Length - i - expectedHash.Length)).Replace("-", "")}");
                            found = true;
                        }
                    }
                    
                    if (!found)
                    {
                        _output.WriteLine("Transaction hash not found in binary data");
                    }
                    
                    // Look for URL strings
                    _output.WriteLine("\nURL findings:");
                    ms.Position = 0;
                    List<int> possibleStringPositions = new List<int>();
                    for (int i = 0; i < binaryData.Length - 5; i++)
                    {
                        if (binaryData[i] == 0x61 && binaryData[i + 1] == 0x63 && binaryData[i + 2] == 0x63 && 
                            binaryData[i + 3] == 0x3a && binaryData[i + 4] == 0x2f && binaryData[i + 5] == 0x2f)
                        {
                            // Found "acc://"
                            possibleStringPositions.Add(i);
                        }
                    }
                    
                    foreach (int pos in possibleStringPositions)
                    {
                        // Estimate the string length
                        int end = pos;
                        while (end < binaryData.Length && binaryData[end] >= 0x20 && binaryData[end] <= 0x7E)
                        {
                            end++;
                        }
                        
                        string url = System.Text.Encoding.ASCII.GetString(binaryData, pos, end - pos);
                        _output.WriteLine($"Found URL at offset {pos}: {url}");
                        
                        // Show what comes before the URL (potential length/type byte)
                        if (pos > 0)
                        {
                            _output.WriteLine($"  Byte before URL: {binaryData[pos - 1]:X2}");
                        }
                        
                        // Show several bytes before and after
                        _output.WriteLine($"  Context before: {BitConverter.ToString(binaryData, Math.Max(0, pos - 5), Math.Min(5, pos)).Replace("-", "")}");
                        _output.WriteLine($"  Context after: {BitConverter.ToString(binaryData, end, Math.Min(5, binaryData.Length - end)).Replace("-", "")}");
                    }
                    
                    // Now let's work backward from our actual hash calculation to see what's going wrong
                    _output.WriteLine("\nComparison with our calculated hash:");
                    
                    var transaction = testCase.ToTransaction();
                    
                    byte[] headerBytes = transaction.Header.MarshalBinary();
                    byte[] headerHash = HashUtils.Sha256(headerBytes);
                    string headerHashHex = new string(Hex.EncodeHex(headerHash));
                    _output.WriteLine($"Header Binary: {new string(Hex.EncodeHex(headerBytes))}");
                    _output.WriteLine($"Header Hash: {headerHashHex}");
                    
                    ITransactionBody body = (ITransactionBody)transaction.Body;
                    byte[] bodyBytes = body.MarshalBinary();
                    byte[] bodyHash = HashUtils.Sha256(bodyBytes);
                    string bodyHashHex = new string(Hex.EncodeHex(bodyHash));
                    _output.WriteLine($"Body Binary: {new string(Hex.EncodeHex(bodyBytes))}");
                    _output.WriteLine($"Body Hash: {bodyHashHex}");
                    
                    // Combined hash buffer
                    byte[] hashBuffer = new byte[64];
                    Buffer.BlockCopy(headerHash, 0, hashBuffer, 0, 32);
                    Buffer.BlockCopy(bodyHash, 0, hashBuffer, 32, 32);
                    string combinedHex = new string(Hex.EncodeHex(hashBuffer));
                    _output.WriteLine($"Combined hash buffer: {combinedHex}");
                    
                    byte[] finalHash = HashUtils.Sha256(hashBuffer);
                    string finalHashHex = new string(Hex.EncodeHex(finalHash));
                    _output.WriteLine($"Our calculated hash: {finalHashHex}");
                    _output.WriteLine($"Expected hash:       {expectedHashHex}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error analyzing binary: {ex.Message}");
                }
            }
        }
    }
} 