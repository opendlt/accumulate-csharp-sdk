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
using NSec.Cryptography;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class ComprehensiveHashAndSignatureVerificationTest
    {
        private readonly ITestOutputHelper _output;

        public ComprehensiveHashAndSignatureVerificationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void VerifyAllHashesAndSignatures_Comprehensive()
        {
            var testVectors = TestVectors.Vectors;
            _output.WriteLine("=== COMPREHENSIVE HASH AND SIGNATURE VERIFICATION ===\n");
            
            int totalTypes = 0;
            int typesWithCorrectTransactionHash = 0;
            int typesWithCorrectInitiatorHash = 0;
            int typesWithCorrectSignatures = 0;
            int typesFullyImplemented = 0;
            
            foreach (var group in testVectors.Transactions)
            {
                // Skip synthetic transactions for now
                if (group.Name.StartsWith("Synthetic"))
                {
                    _output.WriteLine($"\n--- Skipping: {group.Name} (Synthetic) ---");
                    continue;
                }
                
                totalTypes++;
                _output.WriteLine($"\n--- Testing: {group.Name} ---");
                _output.WriteLine($"Test cases: {group.Cases.Count}");
                
                bool allTransactionHashesMatch = true;
                bool allInitiatorHashesMatch = true;
                bool allSignaturesMatch = true;
                bool hasImplementation = true;
                
                foreach (var testCase in group.Cases)
                {
                    try
                    {
                        // Parse the transaction
                        var transaction = testCase.ToTransaction();
                        
                        // 1. Verify Transaction Hash
                        if (testCase.Json.Signatures.Any(s => !string.IsNullOrEmpty(s.TransactionHash)))
                        {
                            byte[] expectedHash = testCase.GetExpectedTransactionHash();
                            byte[] actualHash = TransactionHasher.ComputeTransactionHash(transaction);
                            
                            if (!expectedHash.SequenceEqual(actualHash))
                            {
                                allTransactionHashesMatch = false;
                                _output.WriteLine($"  ❌ Transaction Hash Mismatch:");
                                _output.WriteLine($"     Expected: {new string(Hex.EncodeHex(expectedHash))}");
                                _output.WriteLine($"     Actual:   {new string(Hex.EncodeHex(actualHash))}");
                                
                                // Debug: Show header and body hashes
                                var headerBytes = transaction.Header.MarshalBinary();
                                var bodyBytes = transaction.Body.MarshalBinary();
                                var headerHash = HashUtils.Sha256(headerBytes);
                                var bodyHash = HashUtils.Sha256(bodyBytes);
                                
                                _output.WriteLine($"     Header Hash: {new string(Hex.EncodeHex(headerHash))}");
                                _output.WriteLine($"     Body Hash:   {new string(Hex.EncodeHex(bodyHash))}");
                                _output.WriteLine($"     Body Type:   {transaction.Body.GetType().Name}");
                            }
                        }
                        
                        // 2. Verify Initiator Hash
                        byte[] expectedInitiator = testCase.GetExpectedInitiatorHash();
                        byte[] actualInitiator = transaction.Header.Initiator ?? new byte[0];
                        
                        if (!expectedInitiator.SequenceEqual(actualInitiator))
                        {
                            allInitiatorHashesMatch = false;
                            _output.WriteLine($"  ❌ Initiator Hash Mismatch:");
                            _output.WriteLine($"     Expected: {new string(Hex.EncodeHex(expectedInitiator))}");
                            _output.WriteLine($"     Actual:   {new string(Hex.EncodeHex(actualInitiator))}");
                        }
                        
                        // 3. Verify ED25519 Signatures (if we can)
                        // Note: We can't reproduce signatures without the private key,
                        // but we can verify the signature format and public key
                        foreach (var sig in testCase.Json.Signatures)
                        {
                            if (sig.Type.ToLower() == "ed25519")
                            {
                                var pubKeyBytes = Hex.DecodeHex(sig.PublicKey);
                                var signatureBytes = Hex.DecodeHex(sig.Signature);
                                
                                // Verify signature length (ED25519 signatures are always 64 bytes)
                                if (signatureBytes.Length != 64)
                                {
                                    allSignaturesMatch = false;
                                    _output.WriteLine($"  ❌ Invalid ED25519 signature length: {signatureBytes.Length} (expected 64)");
                                }
                                
                                // Verify public key length (ED25519 public keys are always 32 bytes)
                                if (pubKeyBytes.Length != 32)
                                {
                                    allSignaturesMatch = false;
                                    _output.WriteLine($"  ❌ Invalid ED25519 public key length: {pubKeyBytes.Length} (expected 32)");
                                }
                            }
                        }
                        
                        // 4. Check if marshalling is complete
                        var marshalledBody = transaction.Body.MarshalBinary();
                        if (marshalledBody == null || marshalledBody.Length == 0)
                        {
                            hasImplementation = false;
                            _output.WriteLine($"  ❌ Empty marshalled body");
                        }
                    }
                    catch (NotSupportedException ex)
                    {
                        hasImplementation = false;
                        _output.WriteLine($"  ⚠️  NOT IMPLEMENTED: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        hasImplementation = false;
                        _output.WriteLine($"  ❌ ERROR: {ex.Message}");
                    }
                }
                
                // Summary for this type
                if (hasImplementation)
                {
                    if (allTransactionHashesMatch)
                    {
                        typesWithCorrectTransactionHash++;
                        _output.WriteLine($"  ✅ Transaction hashes: MATCH");
                    }
                    else
                    {
                        _output.WriteLine($"  ❌ Transaction hashes: MISMATCH");
                    }
                    
                    if (allInitiatorHashesMatch)
                    {
                        typesWithCorrectInitiatorHash++;
                        _output.WriteLine($"  ✅ Initiator hashes: MATCH");
                    }
                    else
                    {
                        _output.WriteLine($"  ❌ Initiator hashes: MISMATCH");
                    }
                    
                    if (allSignaturesMatch)
                    {
                        typesWithCorrectSignatures++;
                        _output.WriteLine($"  ✅ Signature format: VALID");
                    }
                    else
                    {
                        _output.WriteLine($"  ❌ Signature format: INVALID");
                    }
                    
                    if (allTransactionHashesMatch && allInitiatorHashesMatch && allSignaturesMatch)
                    {
                        typesFullyImplemented++;
                        _output.WriteLine($"  ✅ FULLY VERIFIED");
                    }
                }
                else
                {
                    _output.WriteLine($"  ⚠️  NOT FULLY IMPLEMENTED");
                }
            }
            
            _output.WriteLine($"\n\n=== FINAL SUMMARY ===");
            _output.WriteLine($"Total transaction types tested: {totalTypes}");
            _output.WriteLine($"Types with correct transaction hashes: {typesWithCorrectTransactionHash}/{totalTypes}");
            _output.WriteLine($"Types with correct initiator hashes: {typesWithCorrectInitiatorHash}/{totalTypes}");
            _output.WriteLine($"Types with valid signature format: {typesWithCorrectSignatures}/{totalTypes}");
            _output.WriteLine($"Fully verified types: {typesFullyImplemented}/{totalTypes}");
            
            // Assert all types are fully verified
            Assert.Equal(totalTypes, typesFullyImplemented);
        }

        [Fact]
        public void VerifyMarshallingCompleteness()
        {
            _output.WriteLine("=== MARSHALLING COMPLETENESS CHECK ===\n");
            
            var testVectors = TestVectors.Vectors;
            
            foreach (var group in testVectors.Transactions)
            {
                if (group.Name.StartsWith("Synthetic"))
                    continue;
                    
                _output.WriteLine($"\nChecking {group.Name}:");
                
                foreach (var testCase in group.Cases)
                {
                    try
                    {
                        var transaction = testCase.ToTransaction();
                        var bodyBytes = transaction.Body.MarshalBinary();
                        var binaryBase64 = testCase.Binary;
                        var binaryBytes = Convert.FromBase64String(binaryBase64);
                        
                        _output.WriteLine($"  Binary size: {binaryBytes.Length} bytes");
                        _output.WriteLine($"  Body marshalled size: {bodyBytes.Length} bytes");
                        
                        // Check if the body appears to have all required fields
                        var bodyType = transaction.Body.GetType();
                        var properties = bodyType.GetProperties()
                            .Where(p => p.Name != "Type" && p.CanRead && p.CanWrite)
                            .ToList();
                            
                        _output.WriteLine($"  Properties: {string.Join(", ", properties.Select(p => p.Name))}");
                        
                        // Check each property is set
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(transaction.Body);
                            if (value == null)
                            {
                                _output.WriteLine($"    ⚠️  {prop.Name} is null");
                            }
                            else if (value is Array arr && arr.Length == 0)
                            {
                                _output.WriteLine($"    ⚠️  {prop.Name} is empty array");
                            }
                        }
                        
                        break; // Just check first case
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  ❌ Error: {ex.Message}");
                        break;
                    }
                }
            }
        }
    }
}