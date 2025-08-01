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
    public class ComprehensiveTransactionTypeTest
    {
        private readonly ITestOutputHelper _output;

        public ComprehensiveTransactionTypeTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestAllTransactionTypes_Comprehensive()
        {
            var testVectors = TestVectors.Vectors;
            _output.WriteLine("=== COMPREHENSIVE TRANSACTION TYPE TEST ===\n");
            
            int totalTransactionTypes = testVectors.Transactions.Count;
            int successfulTypes = 0;
            int failedTypes = 0;
            
            foreach (var group in testVectors.Transactions)
            {
                _output.WriteLine($"\n--- Testing: {group.Name} ---");
                _output.WriteLine($"Test cases: {group.Cases.Count}");
                
                int passedCases = 0;
                int failedCases = 0;
                bool typeImplemented = true;
                
                foreach (var testCase in group.Cases)
                {
                    try
                    {
                        // Try to parse the transaction
                        var transaction = testCase.ToTransaction();
                        
                        // Compute the hash
                        byte[] expectedHash = testCase.GetExpectedTransactionHash();
                        byte[] actualHash = TransactionHasher.ComputeTransactionHash(transaction);
                        
                        // Check if hashes match
                        bool hashesMatch;
                        if (expectedHash.Length == 0)
                        {
                            // For transactions without expected hash (like SignPending), 
                            // just verify we can compute a hash
                            hashesMatch = actualHash.Length > 0;
                        }
                        else
                        {
                            hashesMatch = expectedHash.SequenceEqual(actualHash);
                        }
                        
                        if (hashesMatch)
                        {
                            passedCases++;
                        }
                        else
                        {
                            failedCases++;
                            _output.WriteLine($"  FAIL: Hash mismatch");
                            _output.WriteLine($"    Expected: {new string(Hex.EncodeHex(expectedHash))}");
                            _output.WriteLine($"    Actual:   {new string(Hex.EncodeHex(actualHash))}");
                        }
                    }
                    catch (NotSupportedException ex)
                    {
                        typeImplemented = false;
                        _output.WriteLine($"  NOT IMPLEMENTED: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        failedCases++;
                        _output.WriteLine($"  ERROR: {ex.Message}");
                    }
                }
                
                if (typeImplemented)
                {
                    if (failedCases == 0 && passedCases > 0)
                    {
                        successfulTypes++;
                        _output.WriteLine($"✅ SUCCESS: All {passedCases} test cases passed");
                    }
                    else
                    {
                        failedTypes++;
                        _output.WriteLine($"❌ FAILURE: {passedCases} passed, {failedCases} failed");
                    }
                }
                else
                {
                    _output.WriteLine($"⚠️  NOT IMPLEMENTED");
                }
            }
            
            _output.WriteLine($"\n\n=== SUMMARY ===");
            _output.WriteLine($"Total transaction types: {totalTransactionTypes}");
            _output.WriteLine($"Successfully implemented: {successfulTypes}");
            _output.WriteLine($"Failed implementation: {failedTypes}");
            _output.WriteLine($"Not implemented: {totalTransactionTypes - successfulTypes - failedTypes}");
            
            // List all working types
            _output.WriteLine($"\n=== WORKING TRANSACTION TYPES ===");
            foreach (var group in testVectors.Transactions)
            {
                bool allPass = true;
                bool anyImplemented = true;
                
                foreach (var testCase in group.Cases)
                {
                    try
                    {
                        var transaction = testCase.ToTransaction();
                        byte[] expectedHash = testCase.GetExpectedTransactionHash();
                        byte[] actualHash = TransactionHasher.ComputeTransactionHash(transaction);
                        
                        if (expectedHash.Length == 0)
                        {
                            // For transactions without expected hash, just verify we can compute a hash
                            if (actualHash.Length == 0)
                                allPass = false;
                        }
                        else if (!expectedHash.SequenceEqual(actualHash))
                        {
                            allPass = false;
                        }
                    }
                    catch (NotSupportedException)
                    {
                        anyImplemented = false;
                        break;
                    }
                    catch
                    {
                        allPass = false;
                    }
                }
                
                if (anyImplemented && allPass)
                {
                    _output.WriteLine($"✅ {group.Name}");
                }
            }
        }
    }
}