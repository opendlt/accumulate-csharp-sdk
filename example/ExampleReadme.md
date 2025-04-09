# Acme.Net SDK Examples

This repository contains example applications demonstrating how to use the Acme.Net SDK to interact with the Accumulate blockchain.

## Example Applications

1. **AcmeExample.cs** - A simple example that demonstrates key generation, creating a token account, and submitting a transaction.
2. **AcmeComplexExample.cs** - A more comprehensive example showing multiple transaction types including token accounts, data accounts, sending tokens, and writing data.

## Prerequisites

- .NET 6.0 SDK or higher
- A Testnet or Mainnet Accumulate node to connect to

## Building and Running Examples

1. Create a new console project:

```bash
dotnet new console -n AcmeExamples
cd AcmeExamples
```

2. Add the Acme.Net SDK package:

```bash
dotnet add package Acme.Net.Sdk
```

3. Copy the example files into your project directory:
   - Copy `AcmeExample.cs` or `AcmeComplexExample.cs` to your project directory (rename to `Program.cs` or update project file accordingly)

4. Build and run the project:

```bash
dotnet build
dotnet run
```

## Important Notes

- These examples connect to the Accumulate Testnet by default. For production use, update the endpoint URLs to point to Mainnet.
- The keys generated in these examples are ephemeral - in a real application, you would want to securely store and manage your keys.
- For Testnet, the `AddCredits` function may not work as expected unless you have a funded account. You may need to use the Testnet faucet or other means to fund your accounts.

## Key Concepts Demonstrated

1. **Key Generation**: Creating cryptographic key pairs for signing transactions
2. **Account Creation**: Creating token accounts and data accounts
3. **Transaction Building**: Using builder pattern to construct transactions
4. **Transaction Signing**: Signing transactions with private keys
5. **Transaction Submission**: Submitting signed transactions to the network
6. **Error Handling**: Proper handling of RPC exceptions and response errors

## Additional Resources

- For more information about the Accumulate blockchain, visit [accumulate.org](https://accumulate.org)
- API Documentation: [docs.accumulate.org](https://docs.accumulate.org)
- Testnet Explorer: [testnet.accumulatenetwork.io/explorer](https://testnet.accumulatenetwork.io/explorer) 