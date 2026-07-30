# Changelog

## [2.3.4] - 2026-07-30

### Added
- **Structured CLI** (`accumulate`): 13 verbs, `--json` emits exactly one envelope
  object on stdout, canonical `ACC_*` error codes with a `retryable` flag, and exit
  codes 0/1/2/3 an agent can branch on without parsing. `accumulate --help --json`
  returns the whole command tree. Defaults to testnet; mainnet requires both
  `--network mainnet` and `ACCUMULATE_ALLOW_MAINNET=1`.
  Conforms to `CLI-SPEC.md`; verified by a shared conformance suite across all five SDKs.
- `llms.txt` and `AGENTS.md` now document the CLI.

## [2.3.3] - 2026-07-29

### Added
- Canonical Accumulate error catalog in `llms-full.txt`: every error code with its
  category, whether a retry is productive (`retryable`), likely causes, the concrete
  fix, and the C# type to catch. Each operation now lists the errors it can raise.
- `.devcontainer/devcontainer.json` pinning this repo's toolchain, defaulting to the
  Kermit testnet and carrying no credentials.

### Fixed
- `AGENTS.md` setup, test and layout paths now match this repository's actual root.
  They previously instructed agents to `cd` into a subdirectory that does not exist
  in a fresh clone, so the very first setup command failed.

## [2.3.2] - 2026-07-29

### Fixed
- **A rejected transaction could be reported as successful.** Submit-time rejections were only detected via `status.codeNum` and a hardcoded message allowlist (`insufficientCredits`, `insufficientBalance`, `invalid signature`) — anything outside it, notably `unauthorized`, fell through to `Success = string.IsNullOrEmpty(submitMessage)`, which returns **true** when the reason is carried on `status.error` rather than a top-level `message`. Submit responses are now checked generically for `status.failed` / `status.error`, and the fall-through no longer reports success for a submission the network did not accept.

### Added
- `llms.txt` documents that custom-token precision is configured on the token issuer and is not 1e8.

## [2.3.1] - 2026-07-28

### Fixed
- All XML documentation warnings (CS1570/CS1572/CS1573/CS1574/CS1584/CS1658/CS0419). The package ships `lib/net9.0/Acme.Net.Sdk.xml` via `GenerateDocumentationFile`, and malformed doc comments degraded the IntelliSense surface that file exists to provide — unescaped generics, `cref` parameter lists carrying nullable annotations, a documented parameter that does not exist, and undocumented parameters.

## [2.3.0] - 2026-07-28

### Added
- `Amount.Token(whole, precision)` / `ToToken(precision)` for **custom tokens**. Custom tokens declare their own precision at creation; the wire format is always base units. Previously `Amount` covered only ACME and credits, so issuing a custom token meant hand-computing a power of ten — and issuing `1000` against a precision-8 token mints `0.00001` tokens, not 1000, while the transaction succeeds either way.

### Changed
- Fleet version alignment: all five Accumulate SDKs now ship 2.3.0 with the same `Amount` surface.

All notable changes to the Accumulate C# SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-06-30

### Added
- `HierarchyProvisioner.EnsurePathAsync` — idempotent, arbitrary-depth identity provisioning. Walks an `acc://adi/a/b/.../leaf` path, creating each missing intermediate as a sub-ADI (signing with the immediate parent as principal at every level) and the final segment as the requested leaf kind. Re-runs create nothing.
- `CustodyMode` / `LevelCustody` / `CustodyPlan` — per-level governance. `InheritParent` (default) signs and pays the whole chain from one key page; `OwnKeyBook` gives a level its own key book + funded key page for independent custody.
- `LeafKind` (DataAccount, TokenAccount, KeyBook, SubAdi) and `LeafOptions`.
- `CreditFunders.FromTokenAccount` — funds `OwnKeyBook` key pages from a lite/ADI token account.
- `TxBody.CreateIdentityInherited` — `createIdentity` body with no key book, so a sub-ADI inherits its parent's authority.
- `examples/v3/Example16_EnsureHierarchyPath` — Kermit on-chain acceptance test (depth-3 inherit, idempotency, mixed custody).

## [1.0.0] - 2026-02-27

### Added
- `Accumulate` facade class with unified V2/V3 client access
- `SmartSigner` with automatic signer version tracking and transaction lifecycle
- `KeyManager` for key page state queries
- `QuickStart` and `AccumulateHelper` convenience classes
- `TxBody` static builders for all transaction types
- `BuildContext` for low-level envelope construction
- `TransactionCodec` binary encoder matching Go protocol wire format
- `AccumulateV2Client` and `AccumulateV3Client` with separate endpoint handling
- `Secp256k1KeyPair` placeholder and `AccountAuthOperationType` / `VoteType` enums
- `CanonicalJson` serializer for deterministic JSON output
- `AccumulateException` for structured error handling
- `IHasCustomHash` interface for WriteData/WriteDataTo Merkle hash
- 13 complete v3 examples covering all common workflows
- ADI-to-ADI transfer example with full signing flow

### Changed
- Protocol enums (`SignatureType`, `TransactionTypeCode`, `AccountType`) now match Go core values with wire-name lookup
- `AsyncRPCClient` rewritten with proper V3 envelope formatting, timestamp normalization, and hex encoding
- `RPCClient` updated with V3 `submit`/`query` methods alongside legacy V2 support
- `RPCResponse` expanded to parse V3 query results, transaction status, and multi-record responses
- `Ed25519Signature` and `BaseSignature` updated for correct V3 signing (initiator hash, metadata hash)
- `SignatureKeyPair` consolidated (removed duplicate `Protocol/SignatureKeyPair.cs` and `Ed25519SignatureKeyPair.cs`)
- `Signer` updated with proper transaction hash computation and V3 envelope building
- `SendTokensBuilder` and `IssueTokensBuilder` simplified to use `TxBody` patterns
- `TransactionBuilder` aligned with V3 envelope structure
- `LiteIdentityPrincipal` and `LiteTokenAccountPrincipal` fixed for correct URL derivation
- README rewritten with comprehensive API documentation

### Removed
- Stale analysis/audit/report markdown files (AUDIT_RESULTS, CODE_REVIEW_REPORT, etc.)
- `CLAUDE.md` and implementation plan files
- Duplicate `SignatureKeyPair` and `Ed25519SignatureKeyPair` classes

## [0.2] - 2025-09-01

### Added
- Initial transaction marshaling for all transaction types
- Ed25519 signing and verification
- V2 JSON-RPC client
- Basic examples

## [0.1] - 2025-06-01

### Added
- Initial SDK scaffold with protocol types and RPC client
