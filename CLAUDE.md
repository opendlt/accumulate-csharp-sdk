# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

### Building
```bash
# Build the entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Build a specific project
dotnet build src/Acme.Net.Sdk/Acme.Net.Sdk.csproj
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test -v n

# Run tests with logger
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~TransactionHasherComputeTests"

# Run tests in a specific project
dotnet test test/Acme.Net.Sdk.Tests/Acme.Net.Sdk.Tests.csproj
```

### Code Quality
```bash
# Format code
dotnet format

# Run code analysis
dotnet build /p:RunAnalyzersDuringBuild=true
```

## Architecture Overview

The Acme.Net SDK is a .NET implementation of the Accumulate Protocol SDK. It provides functionality to interact with the Accumulate blockchain network.

### Key Components

1. **Protocol Layer** (`src/Acme.Net.Sdk/Protocol/`)
   - Core protocol types and interfaces
   - Transaction types and structures
   - URL handling for Accumulate addresses

2. **Generated Code** (`src/Acme.Net.Sdk/Protocol/Generated/`)
   - Auto-generated protocol types
   - Transaction body implementations
   - Marshalling/unmarshalling logic

3. **Signing** (`src/Acme.Net.Sdk/Signing/`)
   - Transaction signing implementations
   - Key pair management
   - Signature verification

4. **RPC Client** (`src/Acme.Net.Sdk/Rpc/`)
   - JSON-RPC client implementation
   - API communication layer

5. **Wallet** (`src/Acme.Net.Sdk/Wallet/`)
   - Wallet management functionality
   - Account storage and retrieval

### Transaction Hash Computation

The SDK computes transaction hashes using the formula:
```
SHA256(SHA256(header) || SHA256(body))
```

This matches the Go reference implementation exactly.

### Test Vectors

Test vectors are located in `test/vectors/protocol.4.json` and contain:
- Binary representations of transactions
- Expected hash values
- Signature data

Note: Test vectors include binary envelope metadata that the SDK doesn't generate during raw hash computation.

## Common Tasks

### Adding a New Transaction Type

1. Define the transaction in the appropriate schema/generation source
2. Generate the code (if applicable)
3. Implement `MarshalBinary()` method
4. Add tests with test vectors
5. Update examples if needed

### Debugging Hash Mismatches

1. Check the `MarshalBinary()` implementation
2. Verify field numbers match the protocol specification
3. Use test vectors to compare expected vs actual bytes
4. Ensure `GetBytes()` is used instead of `ToArray()` for marshaller

### Running Examples

```bash
# Build and run an example
dotnet run --project examples/AcmeExample/AcmeExample.csproj

# Build all examples
for proj in examples/*/*.csproj; do dotnet build "$proj"; done
```

## Important Notes

- The SDK targets .NET 9.0
- All projects should use the same target framework
- Test projects include `NoWarn` for nullable reference warnings
- Examples demonstrate proper SDK usage patterns