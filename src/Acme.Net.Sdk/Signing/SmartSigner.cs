using System.Security.Cryptography;
using System.Text.Json;
using Acme.Net.Sdk.Codec;
using Acme.Net.Sdk.Exceptions;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.V3;
using NSec.Cryptography;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Result of a signed and submitted transaction.
    /// </summary>
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string? TxId { get; set; }
        public string? Error { get; set; }
        public JsonElement? Response { get; set; }
    }

    /// <summary>
    /// High-level signer with auto-version tracking, sign+submit+wait convenience.
    /// Matches Dart smart_signer.dart and Python convenience.py SmartSigner.
    /// </summary>
    public class SmartSigner
    {
        private readonly AccumulateV3Client _client;
        private readonly SignatureKeyPair _keypair;
        private int? _cachedVersion;
        private long? _cachedCredits;

        /// <summary>
        /// The signer URL (key page URL for ADIs, lite identity URL for lite accounts).
        /// </summary>
        public string SignerUrl { get; }

        /// <summary>
        /// The signature algorithm (currently always ED25519).
        /// </summary>
        public SignatureType Algorithm => _keypair.Type;

        public SmartSigner(AccumulateV3Client client, SignatureKeyPair keypair, string signerUrl)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _keypair = keypair ?? throw new ArgumentNullException(nameof(keypair));
            SignerUrl = signerUrl ?? throw new ArgumentNullException(nameof(signerUrl));
        }

        /// <summary>
        /// Query and cache the signer version (key page version for ADI, 1 for lite).
        /// </summary>
        public async Task<int> GetSignerVersionAsync(bool refresh = false)
        {
            if (_cachedVersion.HasValue && !refresh)
                return _cachedVersion.Value;

            try
            {
                var result = await _client.QueryAccountAsync(SignerUrl).ConfigureAwait(false);
                if (result.TryGetProperty("account", out var account))
                {
                    if (account.TryGetProperty("version", out var version))
                    {
                        _cachedVersion = version.GetInt32();
                        return _cachedVersion.Value;
                    }
                    // For lite identities there may be no version
                    if (account.TryGetProperty("type", out var type) &&
                        type.GetString() == "liteIdentity")
                    {
                        _cachedVersion = 1;
                        return 1;
                    }
                }
            }
            catch
            {
                // Fallback for lite accounts that don't exist yet
            }

            _cachedVersion = 1;
            return 1;
        }

        /// <summary>
        /// Query and cache the credit balance of the signer.
        /// </summary>
        public async Task<long> GetCreditsAsync(bool refresh = false)
        {
            if (_cachedCredits.HasValue && !refresh)
                return _cachedCredits.Value;

            try
            {
                var result = await _client.QueryAccountAsync(SignerUrl).ConfigureAwait(false);
                if (result.TryGetProperty("account", out var account) &&
                    account.TryGetProperty("creditBalance", out var credits))
                {
                    var credStr = credits.GetRawText().Trim('"');
                    if (long.TryParse(credStr, out var creditBalance))
                    {
                        _cachedCredits = creditBalance;
                        return creditBalance;
                    }
                }
            }
            catch
            {
                // Account may not exist yet
            }

            _cachedCredits = 0;
            return 0;
        }

        /// <summary>
        /// Invalidate the cached signer version and credit balance.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedVersion = null;
            _cachedCredits = null;
        }

        /// <summary>
        /// Captured signer metadata for one signature. The MetadataHash is used BOTH as the
        /// transaction header initiator (for the initiating signer) AND as the signing preimage
        /// input — matching Go core (pkg/build/signature.go sets Header.Initiator = sig.Metadata().Hash()).
        /// Timestamp + Version are captured so the signature object reuses the EXACT values that
        /// went into the hash.
        /// </summary>
        public sealed record SignerMetadata(
            byte[] MetadataHash, long Timestamp, int Version, int Vote,
            string? Memo, byte[]? Data, byte[] PublicKey);

        /// <summary>
        /// Compute this signer's metadata + metadata hash. The hash includes vote/memo/data
        /// (Go's Metadata() clears only Signature + TransactionHash).
        /// </summary>
        public async Task<SignerMetadata> ComputeMetadataAsync(
            string? signatureMemo = null, byte[]? signatureData = null, VoteType? vote = null)
        {
            var version = await GetSignerVersionAsync().ConfigureAwait(false);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
            var publicKey = _keypair.GetPublicKey();
            int voteInt = vote.HasValue ? (int)vote.Value : 0;

            var metadataHash = TransactionCodec.ComputeSignatureMetadataHash(
                publicKey, SignerUrl, version, timestamp,
                signatureType: (int)Algorithm, vote: voteInt,
                memo: signatureMemo, data: signatureData);

            return new SignerMetadata(metadataHash, timestamp, version, voteInt, signatureMemo, signatureData, publicKey);
        }

        /// <summary>
        /// Produce a signed signature object for an already-computed transaction hash, using
        /// previously captured metadata. Used for BOTH the initiator and any co-signers (M-of-N):
        /// every signer signs the SAME txHash but with their own metadata hash.
        /// </summary>
        public Dictionary<string, object?> BuildSignature(byte[] txHash, SignerMetadata meta)
        {
            var preimage = TransactionCodec.CreateSigningPreimage(meta.MetadataHash, txHash);
            var signatureBytes = SignatureAlgorithm.Ed25519.Sign(_keypair.GetKey(), preimage);

            var signature = new Dictionary<string, object?>
            {
                ["type"] = Algorithm.GetWireName(),
                ["publicKey"] = Convert.ToHexString(meta.PublicKey).ToLowerInvariant(),
                ["signature"] = Convert.ToHexString(signatureBytes).ToLowerInvariant(),
                ["signer"] = SignerUrl,
                ["signerVersion"] = meta.Version,
                ["timestamp"] = meta.Timestamp,
                ["transactionHash"] = Convert.ToHexString(txHash).ToLowerInvariant(),
            };
            if (meta.Vote != 0) signature["vote"] = meta.Vote;
            if (!string.IsNullOrEmpty(meta.Memo)) signature["memo"] = meta.Memo;               // signature memo (tag 9)
            if (meta.Data is { Length: > 0 })
                signature["data"] = Convert.ToHexString(meta.Data).ToLowerInvariant();          // signature data (tag 10)
            return signature;
        }

        /// <summary>
        /// Build the transaction header for an initiating signer. Exposed so multi-sig flows can
        /// build the canonical header once and have every co-signer sign the identical txHash.
        /// </summary>
        public static Dictionary<string, object?> BuildHeader(
            string principal, byte[] initiatorMetadataHash, string? memo = null, byte[]? metadata = null)
        {
            var header = new Dictionary<string, object?>
            {
                ["principal"] = principal,
                ["initiator"] = Convert.ToHexString(initiatorMetadataHash).ToLowerInvariant(),
            };
            if (memo != null) header["memo"] = memo;
            if (metadata is { Length: > 0 })
                header["metadata"] = Convert.ToHexString(metadata).ToLowerInvariant();           // header metadata (tag 4)
            return header;
        }

        /// <summary>
        /// Sign a transaction and return the envelope JSON (without submitting).
        /// Now supports: header memo, signature vote, signature memo/data, and header metadata.
        /// </summary>
        public async Task<Dictionary<string, object?>> SignAsync(
            string principal,
            Dictionary<string, object?> body,
            string? memo = null,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null,
            byte[]? headerMetadata = null)
        {
            var meta = await ComputeMetadataAsync(signatureMemo, signatureData, vote).ConfigureAwait(false);
            var header = BuildHeader(principal, meta.MetadataHash, memo, headerMetadata);
            var txHash = TransactionCodec.ComputeTransactionHash(header, body);
            var signature = BuildSignature(txHash, meta);

            return new Dictionary<string, object?>
            {
                ["signatures"] = new List<object?> { signature },
                ["transaction"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["header"] = header, ["body"] = body }
                },
            };
        }

        /// <summary>
        /// Sign a transaction and submit it to the network.
        /// Returns the raw JSON response from the submission.
        /// </summary>
        public async Task<JsonElement> SignAndSubmitAsync(
            string principal,
            Dictionary<string, object?> body,
            string? memo = null,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null,
            byte[]? headerMetadata = null)
        {
            var envelope = await SignAsync(principal, body, memo, vote, signatureMemo, signatureData, headerMetadata)
                .ConfigureAwait(false);
            var results = await _client.SubmitAsync(envelope).ConfigureAwait(false);
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>
        /// Sign, submit, and wait for the transaction to be confirmed.
        /// </summary>
        public async Task<TransactionResult> SignSubmitAndWaitAsync(
            string principal,
            Dictionary<string, object?> body,
            string? memo = null,
            VoteType? vote = null,
            int maxAttempts = 30,
            TimeSpan? pollInterval = null,
            string? signatureMemo = null,
            byte[]? signatureData = null,
            byte[]? headerMetadata = null)
        {
            var interval = pollInterval ?? TimeSpan.FromSeconds(2);

            JsonElement submitResult;
            try
            {
                submitResult = await SignAndSubmitAsync(principal, body, memo, vote, signatureMemo, signatureData, headerMetadata)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new TransactionResult
                {
                    Success = false,
                    Error = $"Submission failed: {ex.Message}",
                };
            }

            // Extract txid and check submission result
            // V3 response format: { "status": { "txID": "acc://...", "code": "ok", ... }, "success": true, "message": "..." }
            string? txId = null;
            string? submitMessage = null;
            bool submitSuccess = false;

            if (submitResult.ValueKind == JsonValueKind.Object)
            {
                // Check for top-level success indicator
                if (submitResult.TryGetProperty("success", out var successProp) &&
                    successProp.ValueKind == JsonValueKind.True)
                    submitSuccess = true;

                // Check for message (may contain warnings/errors)
                if (submitResult.TryGetProperty("message", out var msgProp))
                    submitMessage = msgProp.GetString();

                // Extract txID from status.txID
                if (submitResult.TryGetProperty("status", out var statusProp) &&
                    statusProp.ValueKind == JsonValueKind.Object)
                {
                    if (statusProp.TryGetProperty("txID", out var statusTxId))
                        txId = statusTxId.GetString();

                    // Check status code
                    if (statusProp.TryGetProperty("codeNum", out var codeNum) &&
                        codeNum.GetInt32() != 200)
                        submitSuccess = false;
                }

                // Generic submit-time rejection: the network accepted the envelope
                // but REJECTED the transaction, reporting it on `status`. Checking
                // only a hardcoded message allowlist missed everything outside it —
                // notably `unauthorized` — and such a rejection then fell through to
                // `Success = string.IsNullOrEmpty(submitMessage)`, i.e. reported
                // SUCCESS for a transaction the network had refused.
                if (submitResult.TryGetProperty("status", out var statusForError) &&
                    statusForError.ValueKind == JsonValueKind.Object)
                {
                    var failed = statusForError.TryGetProperty("failed", out var failedProp) &&
                                 failedProp.ValueKind == JsonValueKind.True;
                    var hasError = statusForError.TryGetProperty("error", out var errProp) &&
                                   errProp.ValueKind != JsonValueKind.Null &&
                                   errProp.ValueKind != JsonValueKind.Undefined;

                    if (failed || hasError)
                    {
                        string errText = submitMessage ?? "transaction rejected at submit";
                        if (hasError)
                        {
                            errText = errProp.TryGetProperty("message", out var em)
                                ? em.GetString() ?? errText
                                : errProp.ToString();
                        }

                        return new TransactionResult
                        {
                            Success = false,
                            TxId = txId,
                            Error = $"Transaction rejected at submit: {errText}",
                            Response = submitResult,
                        };
                    }
                }

                // Message-based fatal errors (kept for responses that report the
                // reason only in `message`).
                if (!string.IsNullOrEmpty(submitMessage) &&
                    (submitMessage.Contains("insufficientCredits", StringComparison.OrdinalIgnoreCase) ||
                     submitMessage.Contains("insufficientBalance", StringComparison.OrdinalIgnoreCase) ||
                     submitMessage.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                     submitMessage.Contains("invalid signature", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TransactionResult
                    {
                        Success = false,
                        TxId = txId,
                        Error = submitMessage,
                        Response = submitResult,
                    };
                }
            }

            // Invalidate cache since we submitted a transaction
            InvalidateCache();

            // If submission was successful, try to poll for confirmation
            // If polling fails (e.g. tx not yet indexed), still return success
            if (submitSuccess && !string.IsNullOrEmpty(txId))
            {
                // Brief poll to confirm delivery
                for (int i = 0; i < maxAttempts; i++)
                {
                    try
                    {
                        var txResult = await _client.QueryTransactionAsync(txId).ConfigureAwait(false);

                        if (txResult.TryGetProperty("status", out var status) &&
                            status.ValueKind == JsonValueKind.Object)
                        {
                            // Check if delivered
                            if (status.TryGetProperty("delivered", out var delivered) &&
                                delivered.ValueKind == JsonValueKind.True)
                            {
                                // Check for execution error
                                if (status.TryGetProperty("error", out var error) &&
                                    error.ValueKind != JsonValueKind.Null &&
                                    error.ValueKind != JsonValueKind.Undefined)
                                {
                                    var errMsg = error.TryGetProperty("message", out var m)
                                        ? m.GetString() ?? "Transaction error"
                                        : error.ToString();
                                    return new TransactionResult
                                    {
                                        Success = false,
                                        TxId = txId,
                                        Error = errMsg,
                                        Response = txResult,
                                    };
                                }

                                return new TransactionResult
                                {
                                    Success = true,
                                    TxId = txId,
                                    Response = txResult,
                                };
                            }
                        }
                    }
                    catch
                    {
                        // Transaction not yet indexed, continue polling
                    }

                    await Task.Delay(interval).ConfigureAwait(false);
                }

                // Polling timed out but submission was accepted - assume success
                // (matches Python SDK behavior)
                return new TransactionResult
                {
                    Success = true,
                    TxId = txId,
                    Response = submitResult,
                };
            }

            // No success indicator and no txId
            // Never report success for a submission the network did not accept.
            // `submitSuccess` is false when codeNum != 200 or no success flag was
            // present; previously this line returned Success = true whenever there
            // simply was no message to report.
            return new TransactionResult
            {
                Success = submitSuccess && string.IsNullOrEmpty(submitMessage),
                TxId = txId,
                Error = submitMessage,
                Response = submitResult,
            };
        }

        // ============================================================
        // Sign-pending / remote signing (independent, asynchronous M-of-N)
        // ============================================================
        //
        // The methods above build AND submit a full transaction. The methods below add THIS
        // signer's signature to a transaction that ALREADY EXISTS on the network (e.g. a pending
        // M-of-N transaction another party initiated). The signer need not be the initiator and
        // need not have the original body — only the transaction hash. The envelope carries a
        // `remote` transaction body (TxBody.Remote) referencing that hash, plus this signer's
        // signature over the SAME hash. This is the building block for independent authorities who
        // hold their own keys and sign at different times / from different processes.

        /// <summary>
        /// Resolve a transaction hash from either a 64-char hex string or an <c>acc://&lt;hash&gt;@authority</c> TxID URL.
        /// </summary>
        private static byte[] ResolveTransactionHash(string transactionHashOrId)
        {
            if (string.IsNullOrWhiteSpace(transactionHashOrId))
                throw new ArgumentException("transaction hash or id is required", nameof(transactionHashOrId));

            var s = transactionHashOrId.Trim();
            if (s.StartsWith("acc://", StringComparison.OrdinalIgnoreCase) || s.Contains('@'))
                return new TxID(s).GetHash();
            return Convert.FromHexString(s);
        }

        /// <summary>
        /// Build (without submitting) a signature-only envelope that adds THIS signer's signature to
        /// an existing transaction identified by <paramref name="transactionHash"/>. Uses a
        /// <c>remote</c> body so the original transaction body is not required. The signer's
        /// vote/memo/data are bound into their own signature.
        /// </summary>
        /// <param name="transactionHash">The 32-byte hash of the transaction being signed.</param>
        /// <param name="principal">The principal (account) of the original transaction — where it is pending.</param>
        /// <param name="vote">Optional vote cast with this signature (accept/reject/abstain). Defaults to accept.</param>
        /// <param name="signatureMemo">Optional memo bound into this signature.</param>
        /// <param name="signatureData">Optional opaque data bound into this signature.</param>
        public async Task<Dictionary<string, object?>> SignRemoteAsync(
            byte[] transactionHash,
            string principal,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null)
        {
            if (transactionHash is not { Length: 32 })
                throw new ArgumentException("transactionHash must be 32 bytes", nameof(transactionHash));
            if (string.IsNullOrEmpty(principal))
                throw new ArgumentException("principal is required", nameof(principal));

            var meta = await ComputeMetadataAsync(signatureMemo, signatureData, vote).ConfigureAwait(false);
            var signature = BuildSignature(transactionHash, meta);

            var txHashHex = Convert.ToHexString(transactionHash).ToLowerInvariant();
            var remoteTransaction = new Dictionary<string, object?>
            {
                ["header"] = new Dictionary<string, object?> { ["principal"] = principal },
                ["body"] = TxBody.Remote(txHashHex),
            };

            return new Dictionary<string, object?>
            {
                ["signatures"] = new List<object?> { signature },
                ["transaction"] = new List<object?> { remoteTransaction },
            };
        }

        /// <summary>
        /// Overload that accepts the transaction as a hex string or an <c>acc://…@…</c> TxID URL.
        /// </summary>
        public Task<Dictionary<string, object?>> SignRemoteAsync(
            string transactionHashOrId,
            string principal,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null)
            => SignRemoteAsync(ResolveTransactionHash(transactionHashOrId), principal, vote, signatureMemo, signatureData);

        /// <summary>
        /// Sign an existing/pending transaction by hash and submit the signature to the network.
        /// </summary>
        public async Task<JsonElement> SignRemoteAndSubmitAsync(
            byte[] transactionHash,
            string principal,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null)
        {
            var envelope = await SignRemoteAsync(transactionHash, principal, vote, signatureMemo, signatureData)
                .ConfigureAwait(false);
            var results = await _client.SubmitAsync(envelope).ConfigureAwait(false);
            InvalidateCache();
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>
        /// Sign an existing/pending transaction by hash, submit the signature, and poll the
        /// transaction until it is delivered (or the threshold is still unmet after polling).
        /// Returns success once the network reports the transaction delivered — i.e. the M-of-N
        /// threshold has been reached including this signature.
        /// </summary>
        public async Task<TransactionResult> SignRemoteSubmitAndWaitAsync(
            byte[] transactionHash,
            string principal,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null,
            int maxAttempts = 30,
            TimeSpan? pollInterval = null)
        {
            var interval = pollInterval ?? TimeSpan.FromSeconds(2);
            var txId = new TxID(new Url($"acc://{Convert.ToHexString(transactionHash).ToLowerInvariant()}@{StripScheme(principal)}")).ToString();

            JsonElement submitResult;
            try
            {
                submitResult = await SignRemoteAndSubmitAsync(transactionHash, principal, vote, signatureMemo, signatureData)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new TransactionResult { Success = false, TxId = txId, Error = $"Signature submission failed: {ex.Message}" };
            }

            // Poll the transaction; "delivered" means the threshold was reached and it executed.
            // A V3 transaction query reports status as a string ("pending"/"delivered") with a
            // statusNo (Delivered=201, Pending=202 — Go core pkg/errors/status.yml), OR (on some
            // responses) as an object with a `delivered` bool. Handle both.
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var txResult = await _client.QueryTransactionAsync(txId).ConfigureAwait(false);
                    if (IsDeliveredStatus(txResult))
                        return new TransactionResult { Success = true, TxId = txId, Response = txResult };
                }
                catch
                {
                    // not yet indexed / still pending — keep polling
                }
                await Task.Delay(interval).ConfigureAwait(false);
            }

            // Submission accepted but threshold not yet met (still pending) — report the signature
            // landed without claiming execution.
            return new TransactionResult { Success = true, TxId = txId, Response = submitResult, Error = "signature accepted; transaction still pending (threshold not yet reached)" };
        }

        private static string StripScheme(string url)
            => url.StartsWith("acc://", StringComparison.OrdinalIgnoreCase) ? url.Substring("acc://".Length) : url;

        /// <summary>
        /// True if a V3 transaction-query record reports the transaction as delivered (executed).
        /// Accepts both the string+statusNo shape (Delivered=201) and the object{delivered:bool} shape.
        /// </summary>
        private static bool IsDeliveredStatus(JsonElement txResult)
        {
            if (txResult.TryGetProperty("statusNo", out var n) && n.ValueKind == JsonValueKind.Number && n.GetInt32() == 201)
                return true;
            if (txResult.TryGetProperty("status", out var status))
            {
                if (status.ValueKind == JsonValueKind.String)
                    return string.Equals(status.GetString(), "delivered", StringComparison.OrdinalIgnoreCase);
                if (status.ValueKind == JsonValueKind.Object &&
                    status.TryGetProperty("delivered", out var delivered) && delivered.ValueKind == JsonValueKind.True)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Co-sign an EXISTING transaction by re-supplying its (initiator-set) header and body. This
        /// signer need not be the initiator: per Go core (pkg/build/signature.go), when the header
        /// already has an initiator a co-signer simply signs <c>transaction.GetHash()</c>. The
        /// resulting envelope carries the full transaction plus this signer's signature; the network
        /// matches it to the pending transaction by hash and aggregates the signature toward the
        /// M-of-N threshold.
        /// <para>
        /// This is the robust independent/asynchronous co-sign path: share the initiator's
        /// <c>Header</c> + <c>Body</c> (e.g. from <c>MultiSig.InitiatedTransaction</c>)
        /// out-of-band; each authority co-signs from its own process. Prefer this over the hash-only
        /// <c>SignRemoteAsync</c>
        /// when the original header+body can be shared.
        /// </para>
        /// </summary>
        public async Task<JsonElement> CoSignAndSubmitAsync(
            Dictionary<string, object?> header,
            Dictionary<string, object?> body,
            VoteType? vote = null,
            string? signatureMemo = null,
            byte[]? signatureData = null)
        {
            var meta = await ComputeMetadataAsync(signatureMemo, signatureData, vote).ConfigureAwait(false);
            var txHash = TransactionCodec.ComputeTransactionHash(header, body);
            var signature = BuildSignature(txHash, meta);

            var envelope = new Dictionary<string, object?>
            {
                ["signatures"] = new List<object?> { signature },
                ["transaction"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["header"] = header, ["body"] = body }
                },
            };

            var results = await _client.SubmitAsync(envelope).ConfigureAwait(false);
            InvalidateCache();
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>
        /// Convenience: add a key to the signer's key page.
        /// </summary>
        public Task<TransactionResult> AddKeyAsync(byte[] newPublicKeyHash)
        {
            var keyHashHex = Convert.ToHexString(newPublicKeyHash).ToLowerInvariant();
            var body = TxBody.UpdateKeyPage(new List<Dictionary<string, object?>>
            {
                TxBody.AddKeyOperation(keyHashHex)
            });
            return SignSubmitAndWaitAsync(SignerUrl, body);
        }

        /// <summary>
        /// Convenience: set the multi-sig threshold on the signer's key page.
        /// </summary>
        public Task<TransactionResult> SetThresholdAsync(int threshold)
        {
            var body = TxBody.UpdateKeyPage(new List<Dictionary<string, object?>>
            {
                TxBody.SetThresholdOperation(threshold)
            });
            return SignSubmitAndWaitAsync(SignerUrl, body);
        }
    }
}
