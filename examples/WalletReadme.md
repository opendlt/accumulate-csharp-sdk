# Acme.Net SDK Wallet Example

This example demonstrates how to use the wallet features of the Acme.Net SDK to securely manage keys and accounts for the Accumulate blockchain.

## What This Example Demonstrates

1. **Wallet Management**
   - Creating and loading wallets
   - Encrypting wallet data with a password
   - Saving and retrieving wallets from storage
   - Listing available wallets
   - Backing up and restoring wallets

2. **Account Management**
   - Creating different types of accounts in a wallet
   - Managing key pairs for accounts
   - Retrieving accounts by URL
   - Working with default keys

3. **Transaction Operations**
   - Signing transactions using wallet accounts
   - Creating token accounts on the blockchain
   - Writing data to the blockchain
   - Sending tokens between accounts

## Key Components Used

### `WalletManager`
Provides centralized management of wallets, including creation, loading, and listing of wallets.

### `Wallet`
Represents a secure collection of accounts and their associated keys. Handles encryption and persistence.

### `Account`
Represents a blockchain account with its keys and signing capabilities.

### `FileSystemWalletStorage`
Stores encrypted wallet data on disk with associated metadata.

### `SigningService` (Custom Helper)
A helper class in the example that simplifies transaction signing using wallet accounts.

## How the Example Works

1. **Wallet Setup**
   - Creates a temporary directory for wallet storage
   - Creates a new wallet or loads an existing one
   - Uses password encryption for security

2. **Account Creation**
   - Generates a lite identity account
   - Creates token and data accounts under the identity
   - Stores all accounts in the wallet

3. **Transaction Examples**
   - Creates a token account on the blockchain
   - Writes data to a data account
   - Sends tokens between accounts
   - All transactions are signed using the keys from the wallet

4. **Wallet Backup & Management**
   - Demonstrates backing up a wallet to another wallet file
   - Shows how to list and manage multiple wallets
   - Accesses wallet metadata

## Running the Example

1. Build and run the `AcmeWalletExample.cs` file:

```bash
dotnet run
```

2. Observe the console output showing the wallet operations.

3. Check the temporary directory (displayed at the start) to see the wallet files created.

## Security Considerations

- In a production environment, you should use strong passwords for wallet encryption
- Consider implementing additional security measures like key rotation
- The temporary directory used in the example is for demonstration only; use a secure location in production
- Be careful with handling private keys and sensitive wallet data

## Additional Features

The example demonstrates basic wallet functionality, but the SDK also supports:

- Importing and exporting keys
- Working with multi-signature accounts
- Key rotation and management
- Different account types and configurations

Refer to the SDK documentation for more advanced wallet features.

## Error Handling

The example includes basic error handling for:
- RPC communication errors
- Wallet operations (create, load, etc.)
- Transaction execution errors

In a production application, you should implement more comprehensive error handling and recovery strategies. 