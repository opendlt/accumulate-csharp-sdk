using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Rpc.Models;
using Acme.Net.Sdk.Support;
using System.Globalization;
using System.Collections.Generic;

namespace Acme.Net.Sdk.Rpc
{
    /// <summary>
    /// Asynchronous RPC client for communicating with the Acme network.
    /// Corresponds to io.accumulatenetwork.sdk.rpc.AsyncRPCClient.
    /// </summary>
    public class AsyncRPCClient : RPCClient
    {
        private static readonly SemaphoreSlim EnvelopeLock = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);

        public AsyncRPCClient() : base() { }
        public AsyncRPCClient(Uri uri) : base(uri) { }

        public virtual async Task<TxResponse> SendTxAsync(ITransactionBody body)
        {
            var rpcMethod   = Rpc.Models.RPCMethod.FromClass(body.GetType());
            var rpcResponse = await SendAsync(rpcMethod, body).ConfigureAwait(false);
            var txResponse  = rpcResponse.AsTransactionResponse();
            ResultReader.CheckForErrors(txResponse);
            return txResponse;
        }

        // -------- ensure interface bodies serialize as concrete JSON & match node wire shape --------
        private static object ToRpcEnvelope(Envelope envelope)
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Url -> string
            static string UrlToString(Url u)
            {
                if (u == null) return string.Empty;
                try
                {
                    var uriObj = u.Uri;
                    if (uriObj is Uri sysUri) return sysUri.ToString();
                    if (uriObj != null) return uriObj.ToString();
                }
                catch { }
                return u.ToString();
            }

            // Robust: accepts ticks, “scaled ticks”, ms, sec, or already μs
            static long ToUnixMicros(object? ts)
            {
                if (ts == null) return 0;

                long v = ts is long l ? l
                        : ts is int i ? i
                        : Convert.ToInt64(ts, CultureInfo.InvariantCulture);

                // Already looks like Unix μs?
                if (v >= 1_000_000_000_000_000L && v < 1_000_000_000_000_000_000L)
                    return v;

                // .NET ticks (100ns) since 0001-01-01
                const long epochTicks   = 621_355_968_000_000_000L; // 1970-01-01
                const long tickToMicro  = 10;                       // 1 tick = 100ns = 0.1 μs

                // A) raw ticks
                if (v >= epochTicks)
                    return (v - epochTicks) / tickToMicro;

                // B) “scaled ticks” (ticks ÷ 10, ÷100, ÷1000, ÷10000)
                long scaled = v;
                int[] factors = { 10, 100, 1000, 10000 };
                foreach (var f in factors)
                {
                    long up = scaled * f;
                    if (up >= epochTicks)
                        return (up - epochTicks) / tickToMicro;
                }

                // C) ms since epoch
                if (v >= 1_000_000_000_000L && v < 1_000_000_000_000_000L)
                    return v * 1000;

                // D) s since epoch
                if (v >= 1_000_000_000L && v < 1_000_000_000_000L)
                    return v * 1_000_000;

                // Fallback: assume μs
                return v;
            }

            static string ToHex(byte[] bytes)
            {
                char[] c = new char[bytes.Length * 2];
                int i = 0;
                foreach (var b in bytes)
                {
                    int hi = (b >> 4) & 0xF, lo = b & 0xF;
                    c[i++] = (char)(hi < 10 ? ('0' + hi) : ('a' + hi - 10));
                    c[i++] = (char)(lo < 10 ? ('0' + lo) : ('a' + lo - 10));
                }
                return new string(c);
            }

            static string HexIfBase64OrBytes(object? v)
            {
                if (v == null) return string.Empty;
                if (v is byte[] bb) return ToHex(bb);
                if (v is string s)
                {
                    bool looksHex = s.Length % 2 == 0 && s.All(ch =>
                        (ch >= '0' && ch <= '9') ||
                        (ch >= 'a' && ch <= 'f') ||
                        (ch >= 'A' && ch <= 'F'));
                    if (looksHex) return s.ToLowerInvariant();
                    try { return ToHex(Convert.FromBase64String(s)); } catch { return s; }
                }
                return v.ToString() ?? string.Empty;
            }

            // Reshape a signature to the node's wire format, omitting null/zero fields
            static object ToWireSignature(object sig, JsonSerializerOptions optsLocal, Func<Url, string> urlToStr, Func<object?, string> b64OrBytesToHex)
            {
                try
                {
                    var t = sig.GetType();

                    object? typeVal            = t.GetProperty("Type")?.GetValue(sig);
                    object? publicKeyVal       = t.GetProperty("PublicKey")?.GetValue(sig);
                    object? signatureBytesVal  = t.GetProperty("SignatureBytes")?.GetValue(sig) ?? t.GetProperty("Signature")?.GetValue(sig);
                    object? signerUrlVal       = t.GetProperty("SignerUrl")?.GetValue(sig)    ?? t.GetProperty("Signer")?.GetValue(sig);
                    object? versionVal         = t.GetProperty("Version")?.GetValue(sig)      ?? t.GetProperty("SignerVersion")?.GetValue(sig);
                    object? timestampVal       = t.GetProperty("Timestamp")?.GetValue(sig);
                    object? txHashVal          = t.GetProperty("TransactionHash")?.GetValue(sig) ?? t.GetProperty("TxHash")?.GetValue(sig);

                    string typeStr = "ed25519";
                    if (typeVal != null)
                    {
                        if (typeVal is int i && i == (int)SignatureType.ED25519) typeStr = "ed25519";
                        else typeStr = typeVal.ToString()!.ToLowerInvariant();
                    }

                    string publicKeyHex = b64OrBytesToHex(publicKeyVal);
                    string signatureHex = b64OrBytesToHex(signatureBytesVal);
                    string txHashHex    = b64OrBytesToHex(txHashVal);

                    string signerStr = signerUrlVal switch
                    {
                        Url u   => urlToStr(u),
                        string s => s,
                        _        => signerUrlVal?.ToString() ?? string.Empty
                    };

                    long tsMicros = ToUnixMicros(timestampVal);

                    var sigObj = new Dictionary<string, object>
                    {
                        ["type"]            = typeStr,
                        ["publicKey"]       = publicKeyHex,
                        ["signature"]       = signatureHex,
                        ["signer"]          = signerStr,
                        ["transactionHash"] = txHashHex
                    };

                    if (versionVal != null) sigObj["signerVersion"] = versionVal;
                    if (tsMicros > 0)       sigObj["timestamp"]     = tsMicros;

                    return sigObj;
                }
                catch
                {
                    // Fallback: serialize concrete runtime signature as-is (still respects null ignore)
                    return JsonSerializer.SerializeToElement(sig, sig.GetType(), optsLocal);
                }
            }

            var txs = envelope.Transactions.Select(t =>
            {
                var header = new
                {
                    principal = t.Header?.Principal is Url pu ? UrlToString(pu) : t.Header?.Principal?.ToString(),
                    initiator = HexIfBase64OrBytes(t.Header?.Initiator),
                    memo      = t.Header?.Memo,      // omitted if null (global options)
                    metadata  = t.Header?.Metadata
                };

                object bodyObj;
                switch (t.Body)
                {
                    case Protocol.Generated.Protocol.SendTokens st:
                        var to = st.Recipients?.Select(r => new
                        {
                            url    = r?.Url is Url ru ? UrlToString(ru) : r?.Url?.ToString(),
                            amount = (r?.Amount ?? 0UL).ToString(CultureInfo.InvariantCulture)
                        }).ToArray();

                        bodyObj = new
                        {
                            type = "sendTokens",
                            to,
                            hash = st.Hash, // omitted if null
                            meta = st.Meta
                        };
                        break;

                    default:
                        bodyObj = JsonSerializer.SerializeToElement((object)t.Body!, t.Body!.GetType(), opts);
                        break;
                }

                return new
                {
                    header,
                    body = bodyObj,
                    hash = t.Hash // omitted if null
                };
            }).ToArray();

            var sigs = envelope.Signatures
                .Select(s => ToWireSignature(s, opts, UrlToString, HexIfBase64OrBytes))
                .ToArray();

            return new
            {
                transaction = txs,
                signatures  = sigs
            };
        }
        // ------------------------------------------------------------------------

        public virtual async Task<object?> SendTxAsync(Envelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            bool lockAcquired = false;
            try
            {
                lockAcquired = await EnvelopeLock.WaitAsync(DefaultLockTimeout).ConfigureAwait(false);
                if (!lockAcquired)
                    throw new TimeoutException($"Failed to acquire envelope lock within {DefaultLockTimeout.TotalSeconds} seconds");

                if (GetType() != typeof(AsyncRPCClient))
                    return await Task.FromResult<object?>(null).ConfigureAwait(false);

                var rpcEnvelope = ToRpcEnvelope(envelope);

                try
                {
                    var sig = envelope.Signatures?.Count > 0 ? envelope.Signatures[0] : null;
                    var tx  = envelope.Transactions?.Count > 0 ? envelope.Transactions[0] : null;

                    if (sig is Acme.Net.Sdk.Signing.ISignature s && tx?.Header?.Initiator is byte[] ini && ini.Length == 32)
                    {
                        // SIMPLE: sha256(encode(signature metadata))
                        var meta    = s.MarshalMetadata();
                        var simple  = System.Security.Cryptography.SHA256.HashData(meta);

                        // MERKLE: Merkle hash over canonical leaves (if supported by the signature)
                        byte[] merkle;
                        try { merkle = s.GetInitiatorHashBuilder().MerkleHash(); }
                        catch { merkle = Array.Empty<byte>(); }

                        Console.WriteLine("[Pre-RPC] Sig.Metadata sha256(simple)=" + Convert.ToHexString(simple).ToLowerInvariant());
                        if (merkle.Length == 32)
                            Console.WriteLine("[Pre-RPC] Sig.Metadata merkle       =" + Convert.ToHexString(merkle).ToLowerInvariant());
                        Console.WriteLine("[Pre-RPC] Header.initiator          =" + Convert.ToHexString(ini).ToLowerInvariant());

                        bool ok = ini.AsSpan().SequenceEqual(simple) || (merkle.Length == 32 && ini.AsSpan().SequenceEqual(merkle));
                        if (!ok)
                            throw new InvalidOperationException("Header.initiator does not match SIMPLE or MERKLE recompute just before RPC.");
                    }
                }
                catch (Exception e)
                {
                    RPCClient.LogSink?.Invoke("[Pre-RPC] initiator check ERROR: " + e.Message);
                    throw; // fail fast—don’t send a known-bad envelope
                }

                // Local Ed25519 verify: same digest the signer used
                try
                {
                    var first = envelope.Signatures?.Count > 0 ? envelope.Signatures[0] : null;
                    if (first is Acme.Net.Sdk.Signing.BaseSignature bs &&
                        bs.PublicKey?.Length == 32 &&
                        bs.SignatureBytes?.Length == 64 &&
                        bs.TransactionHash?.Length == 32)
                    {
                        // Rebuild sig-metadata hash the same way the signer did
                        var sig = (Acme.Net.Sdk.Signing.ISignature)first;
                        var meta = sig.MarshalMetadata();                             // encode(signature metadata)
                        var sigMdHash = System.Security.Cryptography.SHA256.HashData(meta);

                        // Combined digest: sha256(sigMdHash || txHash)
                        var concat = new byte[sigMdHash.Length + bs.TransactionHash.Length];
                        Buffer.BlockCopy(sigMdHash, 0, concat, 0, sigMdHash.Length);
                        Buffer.BlockCopy(bs.TransactionHash, 0, concat, sigMdHash.Length, bs.TransactionHash.Length);
                        var toVerify = System.Security.Cryptography.SHA256.HashData(concat);

                        // Verify signature over the combined digest
                        var exec = new Acme.Net.Sdk.Signing.Ed25519SignatureExecutor();
                        bool ok = exec.Verify(toVerify, bs.SignatureBytes, bs.PublicKey);
                        RPCClient.LogSink?.Invoke($"Local Ed25519 verify: {(ok ? "OK" : "FAILED")}");
                    }
                }
                catch (Exception ve)
                {
                    RPCClient.LogSink?.Invoke($"Local Ed25519 verify: ERROR {ve.GetType().Name}: {ve.Message}");
                }


                // TIP: during troubleshooting, you can enable dry-run validation:
                // var rpcParams = new { envelope = rpcEnvelope, checkOnly = true };
                var rpcParams = new { envelope = rpcEnvelope };

                var rpcResponse = SendInternalSync(Rpc.Models.RPCMethod.ExecuteDirect, rpcParams);
                var txResponse  = rpcResponse.AsTransactionResponse();

                ResultReader.CheckForErrors(txResponse);

                if (txResponse.Result == null) return null;

                object transactionStatus;
                try { transactionStatus = ResultReader.ReadValue<TransactionStatus>(txResponse.Result.Value); }
                catch { transactionStatus = txResponse.Result.Value; }

                ResultReader.CheckForErrors(txResponse, transactionStatus);
                return transactionStatus;
            }
            finally
            {
                if (lockAcquired) EnvelopeLock.Release();
            }
        }

        public virtual async Task<object?> SendTxAsync(EnvelopeBuilder envelopeBuilder)
        {
            if (envelopeBuilder == null) throw new ArgumentNullException(nameof(envelopeBuilder));
            return await SendTxAsync(envelopeBuilder.Build()).ConfigureAwait(false);
        }

        public Task<RPCResponse> SendAsync(IRPCBody payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var rpcMethod = Rpc.Models.RPCMethod.FromClass(payload.GetType());
            return SendInternalAsync(rpcMethod, payload);
        }

        public Task<RPCResponse> SendAsync(Rpc.Models.RPCMethod rpcMethod, IRPCBody payload)
        {
            if (rpcMethod == null) throw new ArgumentNullException(nameof(rpcMethod));
            return SendInternalAsync(rpcMethod, payload);
        }

        private async Task<RPCResponse> SendInternalAsync(Rpc.Models.RPCMethod rpcMethod, object body)
        {
            try
            {
                int requestId = NewRequestId();
                var request   = BuildRequest(requestId, rpcMethod, body);
                var response  = await _httpClient.SendAsync(request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw BuildResponseException(response);

                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return RPCResponse.From(responseContent);
            }
            catch (RPCException) { throw; }
            catch (Exception ex) { throw BuildRequestException(ex); }
        }
    }
}
