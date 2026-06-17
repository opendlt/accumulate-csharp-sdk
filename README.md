# OpenDLT Accumulate C# SDK

Production-ready .NET SDK for the Accumulate blockchain protocol. Supports V2 and V3 JSON-RPC APIs, Ed25519 signing, automatic signer version tracking via SmartSigner, and a complete set of transaction builders for all account and token operations.

## Features

- **Smart Signing**: Automatic signer version tracking with `SmartSigner`
- **Complete Protocol Coverage**: All transaction types including identity, token, data, and key management operations
- **Dual API Support**: Both V2 and V3 JSON-RPC endpoints through a unified `Accumulate` client
- **Transaction Builders**: Static `TxBody` factory methods for all transaction types
- **Key Management**: `KeyManager` for querying key page state and managing multi-sig configurations
- **Helper Utilities**: `AccumulateHelper` for balance polling, oracle queries, and credit math
- **Canonical JSON**: Deterministic JSON serialization for protocol compatibility
- **Network Ready**: Mainnet, Testnet (Kermit), and local DevNet support
- **Cross-Platform**: Runs on any platform supporting .NET 9.0

## Requirements

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

## Installation

Clone the repository and build:

```bash
git clone --recursive https://github.com/opendlt/accumulate-csharp-sdk.git
cd accumulate-csharp-sdk
dotnet build src/Acme.Net.Sdk/Acme.Net.Sdk.csproj
```

To reference from your own project:

```bash
dotnet add reference path/to/src/Acme.Net.Sdk/Acme.Net.Sdk.csproj
```

### Submodule Setup

Test vectors are stored in a git submodule. If you cloned without `--recursive`:

```bash
git submodule add https://gitlab.com/accumulatenetwork/sdk/test-data.git test/vectors
git submodule update --init --recursive
```

## Quick Start

```csharp
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;

// Connect to Kermit testnet
using var client = new Accumulate("https://kermit.accumulatenetwork.io");

// Generate key pair and derive lite account URLs
var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
var lid = Principal.ComputeUrl(kp.GetPublicKey());
var lta = Principal.ComputeUrl(kp.GetPublicKey(), new Url("acc://ACME"));

Console.WriteLine($"Lite Identity: {lid}");
Console.WriteLine($"Lite Token Account: {lta}");

// Query account
var account = await client.V3.QueryAccountAsync(lta.String());
```

## Smart Signing API

The `SmartSigner` class handles signer version tracking, transaction hashing, signing, submission, and delivery polling in a single call:

```csharp
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;

// Connect to testnet
using var client = new Accumulate("https://kermit.accumulatenetwork.io");

var kp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
var lid = Principal.ComputeUrl(kp.GetPublicKey());
var lta = Principal.ComputeUrl(kp.GetPublicKey(), new Url("acc://ACME"));

// Create SmartSigner - automatically queries and tracks signer version
var signer = new SmartSigner(client.V3, kp, lid.String());

// Sign, submit, and wait for delivery in one call
var result = await signer.SignSubmitAndWaitAsync(
    principal: lta.String(),
    body: TxBody.SendTokensSingle(
        toUrl: "acc://recipient.acme/tokens",
        amount: "100000000"  // 1 ACME
    ),
    memo: "Payment"
);

if (result.Success)
    Console.WriteLine($"Transaction delivered: {result.TxId}");
```

### SmartSigner Methods

| Method | Description |
|--------|-------------|
| `SignAsync` | Sign a transaction and return the envelope without submitting |
| `SignAndSubmitAsync` | Sign and submit, return the raw response |
| `SignSubmitAndWaitAsync` | Sign, submit, and poll until delivered or timeout |
| `SignRemoteAsync` | Sign an EXISTING/pending transaction by hash (sign-pending); returns a `remote`-body envelope |
| `SignRemoteAndSubmitAsync` | Sign a pending transaction by hash and submit the signature |
| `SignRemoteSubmitAndWaitAsync` | Sign a pending transaction by hash, submit, and poll until delivered |
| `AddKeyAsync` | Add a key to the signer's key page |
| `SetThresholdAsync` | Set the multi-sig threshold on the signer's key page |
| `GetSignerVersionAsync` | Query and cache the signer version |
| `InvalidateCache` | Clear cached version and credit balance |

### Multi-signature (M-of-N) — synchronous and asynchronous

`MultiSig` co-signs one transaction across an M-of-N key page. Two flows:

- **Synchronous co-sign** — all signers' keys available in one process (custodial):
  `MultiSig.SubmitAsync(client, principal, body, initiator, coSigners, …)`.
- **Asynchronous / independent** — each authority holds its own key and signs at a different
  time/process. The initiator makes the transaction *pending* and shares only its hash; each
  authority signs by hash:

```csharp
// Initiator: build + submit, leaving the tx pending until the threshold is met.
var initiated = await MultiSig.InitiateAsync(principal, body, initiator);
await client.V3.SubmitAsync(initiated.Envelope);
// Share initiated.TransactionHash + initiated.Principal out-of-band.

// Each independent authority later (no original body needed — only the hash):
var result = await signerB.SignRemoteSubmitAndWaitAsync(
    initiated.TransactionHash, initiated.Principal,
    vote: VoteType.Accept, signatureMemo: "compliance approved");
```

Query progress with `client.V3.QueryPendingAsync(principal)` and `QueryTransactionAsync(txid)`.
See `examples/v3/Example15_SignPendingMultisig` for the full pending→delivered round-trip on Kermit,
and `Example14_MemoMetadataMultisig` for the synchronous co-sign.

## Transaction Builders

Build transaction bodies using the static `TxBody` factory class. Each method returns a `Dictionary<string, object?>` matching the Accumulate JSON wire format:

```csharp
using Acme.Net.Sdk.Transactions;

// Send tokens
TxBody.SendTokensSingle("acc://recipient/tokens", "100000000");

// Send to multiple recipients
TxBody.SendTokens(new List<TxRecipient>
{
    new("acc://alice/tokens", "50000000"),
    new("acc://bob/tokens", "50000000"),
});

// Add credits (requires oracle price)
TxBody.AddCredits("acc://my-identity", creditAmount.ToString(), oracle);

// Create ADI
TxBody.CreateIdentity("acc://my-adi.acme", "acc://my-adi.acme/book", publicKeyHashHex);

// Create token account
TxBody.CreateTokenAccount("acc://my-adi.acme/tokens", "acc://ACME");

// Create custom token
TxBody.CreateToken("acc://my-adi.acme/mytoken", "MTK", precision: 8,
    supplyLimit: "100000000000000");

// Issue tokens (from token issuer)
TxBody.IssueTokens("acc://my-adi.acme/token-acct", "50000000000");

// Burn tokens
TxBody.BurnTokens("1000000");

// Create data account
TxBody.CreateDataAccount("acc://my-adi.acme/data");

// Write data (hex-encoded entries)
TxBody.WriteData(new List<string> { dataHex });

// Write data to a specific account
TxBody.WriteDataTo("acc://recipient.acme/data", new List<string> { dataHex });

// Key page operations
TxBody.UpdateKeyPage(new List<Dictionary<string, object?>>
{
    TxBody.AddKeyOperation(keyHashHex),
    TxBody.SetThresholdOperation(2),
});

// Create key book and key page
TxBody.CreateKeyBook("acc://my-adi.acme/book2", publicKeyHashHex);
TxBody.CreateKeyPage(new List<KeySpecParams> { new(keyHashHex) });

// Account auth
TxBody.UpdateAccountAuth(operations);
TxBody.LockAccount(height: 100);

// Faucet
TxBody.AcmeFaucet("acc://lite-token-account/acme");
```

## Helper Utilities

The `AccumulateHelper` class provides mid-level convenience methods:

```csharp
using Acme.Net.Sdk.Helpers;

var helper = new AccumulateHelper(client);

// Poll until balance appears (with timeout)
long balance = await helper.PollForBalanceAsync(ltaUrl, timeout: TimeSpan.FromSeconds(60));

// Query balance and credits
long bal = await helper.GetBalanceAsync("acc://my-adi.acme/tokens");
long credits = await helper.GetCreditsAsync("acc://my-adi.acme/book/1");

// Get oracle price and compute credit costs
int oracle = await helper.GetOracleAsync();
long acmeAmount = AccumulateHelper.CreditsToAcme(10000, oracle);

// Wait for a transaction to be delivered
var txResult = await helper.WaitForTxAsync(txId, timeout: TimeSpan.FromSeconds(30));
```

## Key Management

The `KeyManager` class queries key page state:

```csharp
using Acme.Net.Sdk.Signing;

var keyManager = new KeyManager(client.V3, "acc://my-adi.acme/book/1");

var state = await keyManager.GetKeyPageStateAsync();
Console.WriteLine($"Version: {state.Version}");
Console.WriteLine($"Threshold: {state.AcceptThreshold}");
Console.WriteLine($"Keys: {state.Keys.Count}");
Console.WriteLine($"Credits: {state.CreditBalance}");

foreach (var key in state.Keys)
    Console.WriteLine($"  Key Hash: {key.KeyHash}");
```

Add keys and set thresholds via SmartSigner:

```csharp
var adiSigner = new SmartSigner(client.V3, adiKeyPair, keyPageUrl);

// Add a new key
var keyHash = SHA256.HashData(newKeyPair.GetPublicKey());
await adiSigner.AddKeyAsync(keyHash);

// Set 2-of-3 multi-sig threshold
await adiSigner.SetThresholdAsync(2);
```

## Network Endpoints

```csharp
using Acme.Net.Sdk;

// Public networks
using var mainnet = new Accumulate("https://mainnet.accumulatenetwork.io");
using var testnet = new Accumulate("https://kermit.accumulatenetwork.io");

// Local development
using var devnet = Accumulate.Devnet();  // http://localhost:26660

// Custom host and port
using var local = Accumulate.Local(port: 26660);
```

The unified `Accumulate` client exposes both API versions:

```csharp
client.V2  // AccumulateV2Client - legacy JSON-RPC (faucet, execute, query)
client.V3  // AccumulateV3Client - current JSON-RPC (submit, query, node-info)
```

## Supported Signature Types

| Type | Enum Value | Wire Name | Status |
|------|-----------|-----------|--------|
| Ed25519 | `ED25519 = 2` | `ed25519` | Full signing support |
| Legacy Ed25519 | `LEGACY_ED25519 = 1` | `legacyED25519` | Protocol defined |
| RCD1 | `RCD1 = 3` | `rcd1` | Protocol defined |
| BTC | `BTC = 4` | `btc` | Protocol defined |
| BTC Legacy | `BTC_LEGACY = 5` | `btcLegacy` | Protocol defined |
| ETH | `ETH = 6` | `eth` | Protocol defined |
| Delegated | `DELEGATED = 7` | `delegated` | Protocol defined |
| RSA-SHA256 | `RSA_SHA256 = 9` | `rsaSha256` | Protocol defined |
| ECDSA-SHA256 | `ECDSA_SHA256 = 10` | `ecdsaSha256` | Protocol defined |

## Examples

Complete working examples are provided in `examples/v3/`. All examples run real on-chain operations against the Kermit testnet:

| Example | Description |
|---------|-------------|
| `Example01_LiteIdentities` | Lite identity creation, faucet funding, credits, lite-to-lite token transfer |
| `Example02_AccumulateIdentities` | ADI creation via SmartSigner |
| `Example03_AdiTokenAccounts` | ADI token account creation and token transfers |
| `Example04_DataAccountsEntries` | Data account creation, writing and querying data entries |
| `Example05_AdiToAdiTransfer` | Full ADI-to-ADI token transfer workflow |
| `Example06_CustomTokens` | Custom token issuer creation, token issuance, and custom token transfers |
| `Example08_QueryTxSignatures` | V3 query APIs: accounts, chains, pending transactions |
| `Example09_KeyManagement` | Key export/import, adding keys to key pages, multi-sig thresholds |
| `Example10_UpdateKeyPageThreshold` | Adding multiple keys and setting 2-of-3 threshold |
| `Example11_MultiSignatureTypes` | Signature type enumeration and wire name lookups |
| `Example12_QuickstartDemo` | Wallet creation, faucet, canonical JSON, network endpoints |
| `Example13_AdiToAdiTransferWithHeaderOptions` | ADI-to-ADI transfer with memo in transaction header |
| `Example14_MemoMetadataMultisig` | Header metadata, signature memo/data, and synchronous M-of-N co-signing |
| `Example15_SignPendingMultisig` | Independent/asynchronous M-of-N: initiate a pending tx, then sign-by-hash to completion |

Run any example:

```bash
dotnet run --project examples/v3/Example01_LiteIdentities/Example01_LiteIdentities.csproj
```

## Project Structure

```
src/Acme.Net.Sdk/
├── Accumulate.cs              # Unified client entry point (V2 + V3)
├── Api/                       # API client implementations
│   └── V2/                    # V2 response models
├── Codec/
│   └── TransactionCodec.cs    # Binary TLV encoding and transaction hashing
├── Core/
│   └── NetworkEndpoint.cs     # Network endpoint configuration
├── Exceptions/                # AccumulateException hierarchy
├── Helpers/
│   ├── AccumulateHelper.cs    # Balance polling, oracle queries, credit math
│   └── QuickStart.cs          # Rapid development helper
├── Protocol/
│   ├── Principal.cs           # Lite identity/token URL derivation
│   ├── SignatureType.cs       # Signature type enum (17 types)
│   ├── TransactionTypeCode.cs # Transaction type codes (33 types)
│   ├── Url.cs                 # Accumulate URL handling
│   ├── VoteType.cs            # Vote type enum
│   ├── Generated/             # Generated protocol classes
│   │   └── Protocol/          # Transaction body types (23 types)
│   └── Signing/               # Low-level transaction signing
├── Rpc/
│   ├── AsyncRPCClient.cs      # Async JSON-RPC transport
│   ├── RPCClient.cs           # Sync JSON-RPC transport
│   └── Models/                # RPC request/response models
├── Signing/
│   ├── AccKeyPairGenerator.cs # Ed25519 key pair generation
│   ├── SignatureKeyPair.cs    # Key pair management
│   ├── SmartSigner.cs         # Auto-version signer with submit+wait
│   ├── KeyManager.cs          # Key page state queries
│   └── Signer.cs              # Low-level signing operations
├── Support/
│   ├── CanonicalJson.cs       # Deterministic JSON serialization
│   ├── Marshaller.cs          # Binary TLV field encoding
│   └── HashUtils.cs           # SHA-256 and Merkle tree utilities
├── Transactions/
│   ├── TxBody.cs              # Static transaction body factory (all types)
│   ├── BuildContext.cs        # Transaction header metadata
│   └── *Builder.cs            # Typed transaction builders
├── V2/
│   └── AccumulateV2Client.cs  # V2 JSON-RPC client
├── V3/
│   └── AccumulateV3Client.cs  # V3 JSON-RPC client
└── Wallet/
    ├── Wallet.cs              # Wallet management with encryption
    └── FileSystemWalletStorage.cs

examples/v3/                   # 12 complete working examples
test/
├── Acme.Net.Sdk.Tests/        # Unit and integration tests
└── vectors/                   # Protocol test vectors (git submodule)
```

## Building

```bash
# Build the SDK
dotnet build src/Acme.Net.Sdk/Acme.Net.Sdk.csproj

# Build and run an example
dotnet run --project examples/v3/Example01_LiteIdentities/Example01_LiteIdentities.csproj
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run test vector verification only
dotnet test --filter FullyQualifiedName~TransactionHasherVectorTests
```

Test vectors are sourced from the shared [test-data](https://gitlab.com/accumulatenetwork/sdk/test-data.git) repository and verify transaction hashing compatibility with the reference Go implementation.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Newtonsoft.Json` | 13.0.3 | JSON serialization |
| `NSec.Cryptography` | 24.4.0 | Ed25519 signing and key derivation |

## Error Handling

```csharp
using Acme.Net.Sdk.Exceptions;
using Acme.Net.Sdk.Signing;

try
{
    var result = await signer.SignSubmitAndWaitAsync(principal, body);
    if (!result.Success)
        Console.WriteLine($"Transaction failed: {result.Error}");
}
catch (AccumulateException ex)
{
    Console.WriteLine($"SDK error: {ex.Message}");
}
catch (Acme.Net.Sdk.Rpc.RPCException ex)
{
    Console.WriteLine($"RPC error: {ex.Message}");
}
```

The `TransactionResult` returned by `SignSubmitAndWaitAsync` contains:

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the transaction was delivered successfully |
| `TxId` | `string?` | The transaction ID (`acc://...` URL) |
| `Error` | `string?` | Error message if the transaction failed |
| `Response` | `JsonElement?` | Raw JSON response from the server |

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Links

- [Accumulate Protocol](https://accumulatenetwork.io/)
- [API Documentation](https://docs.accumulatenetwork.io/)
- [Kermit Testnet Explorer](https://kermit.explorer.accumulatenetwork.io/)
