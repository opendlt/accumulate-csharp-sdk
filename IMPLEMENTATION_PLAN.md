# Implementation Plan to Fix Test Vector Matching

## Problem Summary

The C# SDK fails to match test vectors because:
1. Test vectors use Accumulate Protocol v3 binary format
2. SDK uses standard Protocol Buffers format
3. Different binary encoding = different hashes and signatures

## Evidence

From the test vector analysis:
- Binary position 163-256 contains the transaction in v3 format
- Header starts with field 11 (0x5d), not standard field 1 (0x0a)
- Body uses field 30 (0xf2), not standard field 2 (0x12)
- The envelope has a custom 3-byte header: 01 9f 01

## Solution Required

### 1. Implement V3 Binary Marshalling

Create new marshalling classes that implement the v3 format:
- `V3Marshaller` - handles v3 binary encoding
- `V3TransactionMarshaller` - specific transaction encoding
- `V3EnvelopeMarshaller` - envelope wrapping

### 2. Update Transaction Classes

Modify all transaction body classes to support v3 marshalling:
- Keep existing protobuf for API compatibility
- Add v3 marshalling methods for test vectors
- Use correct field numbers for v3 format

### 3. Fix Transaction Hash Calculation

The hash must be calculated on v3 format:
- Wrap transaction in v3 envelope
- Use exact byte layout from test vectors
- Hash the v3-encoded fields

### 4. Fix Signature Generation

Signatures must sign the v3 binary data:
- Use v3 format for transaction hashing
- Ensure deterministic signatures match

## Next Steps

1. Study the Go SDK implementation for v3 format details
2. Implement V3Marshaller classes
3. Update all transaction bodies
4. Fix hash calculation
5. Fix signature generation
6. Verify all test vectors pass

## Test Vector Structure (Decoded)

```
Position 0-2:    Custom header (01 9f 01)
Position 3-162:  Envelope metadata and signatures
Position 163-213: Transaction header (v3 format)
Position 214-255: Transaction body (v3 format)
Position 130-161: Transaction hash (matches expected)
```

The transaction at 163-256 uses:
- Non-standard field numbers
- Custom encoding rules
- Must be replicated exactly for tests to pass