# Test Vector Analysis Report

## Summary

After extensive analysis of the test vectors, I've discovered that the SDK does not match the test vectors because they use different binary encoding formats.

## Key Findings

1. **Test Vector Format**: The test vectors use Accumulate Protocol v3 binary format, which is a custom encoding that differs from standard Protocol Buffers.

2. **Binary Structure**: The test vector binary has the following structure:
   - Bytes 0-2: Custom header (01 9f 01)
   - Bytes 3-162: Envelope wrapper containing signatures and metadata
   - Bytes 163-256: Transaction data in custom format

3. **Transaction Encoding**: The transaction at position 163-256 uses:
   - Field 11 (0x5d): A fixed32 timestamp/version field
   - Custom field numbers and encoding for body (field 30 instead of field 2)
   - Non-standard protobuf structure

4. **Hash Calculation**: The expected hash can be found at position 130-161 in the binary, confirming it uses this custom format.

## Root Cause

The SDK implements standard Protocol Buffers encoding, while the test vectors expect Accumulate v3 binary format. This is why:
- Transaction hashes don't match
- Signatures don't match (they sign different binary data)
- The binary output is completely different

## Solution Required

To match the test vectors exactly, the SDK needs to:

1. Implement Accumulate v3 binary marshalling format
2. Use the exact field numbers and encoding structure from the test vectors
3. Ensure all binary output matches bit-for-bit

## Test Vector Transaction Structure

Based on the analysis, the test vector transaction uses:
- Header: Principal (field 1) + Initiator (field 2) 
- Body: Type (field 1) + URL (field 2) + TokenURL (field 3)
- Special encoding with field 30 for the body in the envelope

## Next Steps

1. Implement v3 binary marshalling in the SDK
2. Update all MarshalBinary methods to use the correct format
3. Ensure transaction hash calculation matches exactly
4. Fix signature generation to sign the correct binary data