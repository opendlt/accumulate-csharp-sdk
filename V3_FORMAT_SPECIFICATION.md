# Accumulate V3 Binary Format Specification

Based on test vector analysis, here's the v3 binary format used by Accumulate:

## Envelope Format

```
Bytes 0-2:   Custom header (01 9f 01)
Bytes 3-n:   Envelope content
  Field 1:   Transaction (wire type 2, length-delimited)
  Field 2:   Signatures (repeated)
  Field 5:   Transaction hash
```

## Transaction Format

The transaction does NOT use standard protobuf Transaction message.
Instead, it uses a custom format:

### Header Section (at position 163)
- Starts with 0x5d (field 11, wire type 5 - fixed32)
- Followed by 4 bytes of data
- Then principal and initiator data

### Body Section (at position 214)  
- Starts with 0xf2 (field 30, wire type 2)
- Length byte
- Body content with custom field numbers

## CreateTokenAccount Body Format

Based on CompareBodyEncodingTest results:
- Field 1: Type (value 2)
- Field 2: URL string
- Field 3: Token URL string

NOT the standard protobuf format!

## Hash Calculation

1. Extract transaction from envelope
2. Split into header and body sections
3. Hash = SHA256(SHA256(header) || SHA256(body))
4. Must use exact v3 binary encoding

## Implementation Notes

- Cannot use standard protobuf encoding
- Must implement custom v3 marshaller
- Field numbers differ from protobuf schema
- Envelope has special 3-byte prefix
- All binary must match exactly for test vectors