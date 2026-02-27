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
        /// Sign a transaction and return the envelope JSON (without submitting).
        /// </summary>
        public async Task<Dictionary<string, object?>> SignAsync(
            string principal,
            Dictionary<string, object?> body,
            string? memo = null,
            VoteType? vote = null)
        {
            var signerVersion = await GetSignerVersionAsync().ConfigureAwait(false);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
            var publicKey = _keypair.GetPublicKey();
            int voteInt = vote.HasValue ? (int)vote.Value : 0;

            // 1. Compute signature metadata hash
            var metadataHash = TransactionCodec.ComputeSignatureMetadataHash(
                publicKey, SignerUrl, signerVersion, timestamp,
                signatureType: (int)Algorithm, vote: voteInt, memo: null, data: null);

            // 2. Build header with initiator = metadataHash hex
            var header = new Dictionary<string, object?>
            {
                ["principal"] = principal,
                ["initiator"] = Convert.ToHexString(metadataHash).ToLowerInvariant(),
            };
            if (memo != null) header["memo"] = memo;

            // 3. Compute transaction hash
            var txHash = TransactionCodec.ComputeTransactionHash(header, body);

            // 4. Create signing preimage = SHA256(metadataHash || txHash)
            var preimage = TransactionCodec.CreateSigningPreimage(metadataHash, txHash);

            // 5. Sign with Ed25519
            var alg = SignatureAlgorithm.Ed25519;
            var key = _keypair.GetKey();
            var signatureBytes = alg.Sign(key, preimage);

            // 6. Build envelope
            var signature = new Dictionary<string, object?>
            {
                ["type"] = Algorithm.GetWireName(),
                ["publicKey"] = Convert.ToHexString(publicKey).ToLowerInvariant(),
                ["signature"] = Convert.ToHexString(signatureBytes).ToLowerInvariant(),
                ["signer"] = SignerUrl,
                ["signerVersion"] = signerVersion,
                ["timestamp"] = timestamp,
                ["transactionHash"] = Convert.ToHexString(txHash).ToLowerInvariant(),
            };
            if (voteInt != 0) signature["vote"] = voteInt;

            var envelope = new Dictionary<string, object?>
            {
                ["signatures"] = new List<object?> { signature },
                ["transaction"] = new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        ["header"] = header,
                        ["body"] = body,
                    }
                },
            };

            return envelope;
        }

        /// <summary>
        /// Sign a transaction and submit it to the network.
        /// Returns the raw JSON response from the submission.
        /// </summary>
        public async Task<JsonElement> SignAndSubmitAsync(
            string principal,
            Dictionary<string, object?> body,
            string? memo = null,
            VoteType? vote = null)
        {
            var envelope = await SignAsync(principal, body, memo, vote).ConfigureAwait(false);
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
            TimeSpan? pollInterval = null)
        {
            var interval = pollInterval ?? TimeSpan.FromSeconds(2);

            JsonElement submitResult;
            try
            {
                submitResult = await SignAndSubmitAsync(principal, body, memo, vote).ConfigureAwait(false);
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

                // Check if the message indicates a fatal error
                if (!string.IsNullOrEmpty(submitMessage) &&
                    (submitMessage.Contains("insufficientCredits", StringComparison.OrdinalIgnoreCase) ||
                     submitMessage.Contains("insufficientBalance", StringComparison.OrdinalIgnoreCase) ||
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
            return new TransactionResult
            {
                Success = string.IsNullOrEmpty(submitMessage),
                TxId = txId,
                Error = submitMessage,
                Response = submitResult,
            };
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
