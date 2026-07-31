using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Acme.Net.Sdk.Exceptions;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Codec
{
    /// <summary>
    /// Implements the Go core's transaction hash algorithm for dictionary-based
    /// (JSON wire format) transaction bodies and headers.
    /// Uses the existing Marshaller class for TLV encoding.
    /// Matches Dart transaction_codec.dart and Python convenience.py.
    /// </summary>
    public static class TransactionCodec
    {
        /// <summary>
        /// Compute the transaction hash from header and body JSON dictionaries.
        /// txHash = SHA256(SHA256(headerTLV) || SHA256(bodyTLV))
        /// For WriteData/WriteDataTo, the body hash uses the entry hash instead of SHA256(bodyTLV).
        /// </summary>
        public static byte[] ComputeTransactionHash(
            Dictionary<string, object?> header,
            Dictionary<string, object?> body)
        {
            var headerBytes = MarshalHeader(header);
            var headerHash = SHA256.HashData(headerBytes);

            // WriteData and WriteDataTo use a special body hash via Merkle DAG
            // (matching Go core and Python SDK behavior)
            var typeName = GetStringValue(body, "type");
            byte[] bodyHash;
            if (typeName == "writeData" || typeName == "writeDataTo")
            {
                bodyHash = ComputeWriteDataBodyHash(body);
            }
            else
            {
                var bodyBytes = MarshalBody(body);
                bodyHash = SHA256.HashData(bodyBytes);
            }

            var concat = new byte[32 + 32];
            Buffer.BlockCopy(headerHash, 0, concat, 0, 32);
            Buffer.BlockCopy(bodyHash, 0, concat, 32, 32);

            return SHA256.HashData(concat);
        }

        /// <summary>
        /// Compute WriteData/WriteDataTo body hash using the special GetHash() algorithm.
        /// Matches Go core and Python SDK _compute_write_data_body_hash().
        ///
        /// Instead of SHA256(MarshalBinary(body)), WriteData uses:
        /// 1. Marshal body WITHOUT the entry field
        /// 2. Compute entry.Hash() separately via Merkle DAG
        /// 3. Return MerkleHash([SHA256(body_without_entry), entry_hash])
        /// </summary>
        private static byte[] ComputeWriteDataBodyHash(Dictionary<string, object?> body)
        {
            var typeName = GetStringValue(body, "type");
            int typeCode = TransactionTypeCode.FromApiName(typeName ?? "writeData");

            // Step 1: Marshal body WITHOUT entry — type + other non-entry fields
            using var m = new Marshaller();
            m.WriteUInt(1, typeCode);

            // For WriteDataTo, include the recipient field (tag 02) in the body hash
            if (typeName == "writeDataTo")
            {
                var recipient = GetStringValue(body, "recipient");
                if (recipient != null) m.WriteUrl(2, new Url(recipient));
            }

            if (body.TryGetValue("scratch", out var scratch) && scratch is bool scratchBool && scratchBool)
                m.WriteBool(3, true);
            if (body.TryGetValue("writeToState", out var wts) && wts is bool wtsBool && wtsBool)
                m.WriteUInt(4, 1);

            var bodyWithoutEntry = m.GetBytes();
            var h1 = SHA256.HashData(bodyWithoutEntry);

            // Step 2: Compute entry hash
            var entryHash = ComputeDataEntryHash(body);

            // Step 3: Merkle hash [h1, entryHash]
            return MerkleDagHash(new List<byte[]> { h1, entryHash });
        }

        /// <summary>
        /// Compute the data entry hash.
        /// For AccumulateDataEntry: MerkleHash of SHA256(data_i) for each segment.
        /// For DoubleHashDataEntry: SHA256(MerkleHash of SHA256(data_i)).
        /// </summary>
        private static byte[] ComputeDataEntryHash(Dictionary<string, object?> body)
        {
            var entry = GetDictValue(body, "entry");
            if (entry == null)
                return new byte[32];

            var entryType = GetStringValue(entry, "type") ?? "doubleHash";
            var data = GetListValue(entry, "data");

            if (data == null || data.Count == 0)
                return new byte[32];

            // Hash each data segment
            var itemHashes = new List<byte[]>();
            foreach (var item in data)
            {
                var hex = item?.ToString();
                if (hex == null) continue;
                var segmentBytes = Convert.FromHexString(hex);
                itemHashes.Add(SHA256.HashData(segmentBytes));
            }

            // Compute Merkle root of segment hashes
            var merkleRoot = MerkleDagHash(itemHashes);

            // DoubleHash: wrap with additional SHA256
            if (entryType is "doubleHash" or "doubleHashDataEntry")
                return SHA256.HashData(merkleRoot);

            return merkleRoot;
        }

        /// <summary>
        /// Compute Merkle DAG root using Go's binary carry addition pattern.
        /// Matches Go's merkle.State.AddEntry + Anchor and Python's _merkle_hash.
        /// </summary>
        private static byte[] MerkleDagHash(List<byte[]> hashes)
        {
            if (hashes.Count == 0)
                return new byte[32];

            var pending = new List<byte[]?>();
            int count = 0;

            foreach (var h in hashes)
            {
                count++;
                // Pad pending to bit_length of count
                int bitLen = BitLength(count);
                while (pending.Count < bitLen)
                    pending.Add(null);

                var current = h;
                for (int i = 0; i < pending.Count; i++)
                {
                    if (pending[i] == null)
                    {
                        pending[i] = current;
                        break;
                    }
                    current = CombineHashes(pending[i]!, current);
                    pending[i] = null;
                }
            }

            // Compute anchor
            byte[]? anchor = null;
            foreach (var v in pending)
            {
                if (anchor == null)
                {
                    if (v != null)
                        anchor = (byte[])v.Clone();
                }
                else if (v != null)
                {
                    anchor = CombineHashes(v, anchor);
                }
            }

            return anchor ?? new byte[32];
        }

        private static byte[] CombineHashes(byte[] a, byte[] b)
        {
            var concat = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, concat, 0, a.Length);
            Buffer.BlockCopy(b, 0, concat, a.Length, b.Length);
            return SHA256.HashData(concat);
        }

        private static int BitLength(int value)
        {
            int bits = 0;
            while (value > 0)
            {
                bits++;
                value >>= 1;
            }
            return bits;
        }

        /// <summary>
        /// Compute signature metadata hash for the initiator.
        /// </summary>
        public static byte[] ComputeSignatureMetadataHash(
            byte[] publicKey,
            string signerUrl,
            int signerVersion,
            long timestamp,
            int signatureType = 2,
            int vote = 0,
            string? memo = null,
            byte[]? data = null)
        {
            var metaBytes = MarshalSignatureMetadata(
                signatureType, publicKey, signerUrl, signerVersion, timestamp, vote, memo, data);
            return SHA256.HashData(metaBytes);
        }

        /// <summary>
        /// Create signing preimage: SHA256(metadataHash || txHash)
        /// </summary>
        public static byte[] CreateSigningPreimage(byte[] metadataHash, byte[] txHash)
        {
            var concat = new byte[metadataHash.Length + txHash.Length];
            Buffer.BlockCopy(metadataHash, 0, concat, 0, metadataHash.Length);
            Buffer.BlockCopy(txHash, 0, concat, metadataHash.Length, txHash.Length);
            return SHA256.HashData(concat);
        }

        /// <summary>
        /// Marshal transaction header to binary TLV.
        /// Field 1: Principal (URL string)
        /// Field 2: Initiator (32-byte hash)
        /// Field 3: Memo (string)
        /// Field 4: Metadata (bytes)
        /// </summary>
        internal static byte[] MarshalHeader(Dictionary<string, object?> header)
        {
            using var m = new Marshaller();

            if (header.TryGetValue("principal", out var principal) && principal is string principalStr)
            {
                m.WriteUrl(1, new Url(principalStr));
            }

            if (header.TryGetValue("initiator", out var initiator) && initiator is string initiatorHex)
            {
                var initBytes = Convert.FromHexString(initiatorHex);
                m.WriteHash(2, initBytes);
            }

            if (header.TryGetValue("memo", out var memo) && memo is string memoStr && !string.IsNullOrEmpty(memoStr))
            {
                m.WriteString(3, memoStr);
            }

            if (header.TryGetValue("metadata", out var metadata) && metadata is string metaHex && !string.IsNullOrEmpty(metaHex))
            {
                m.WriteBytes(4, Convert.FromHexString(metaHex));
            }

            return m.GetBytes();
        }

        /// <summary>
        /// Marshal transaction body to binary TLV.
        /// Field 1: TransactionType (uvarint)
        /// Remaining fields depend on the transaction type.
        /// </summary>
        internal static byte[] MarshalBody(Dictionary<string, object?> body)
        {
            var typeName = GetStringValue(body, "type")
                ?? throw new AccumulateEncodingException("Transaction body must have a 'type' field");

            int typeCode = TransactionTypeCode.FromApiName(typeName);
            if (typeCode == TransactionTypeCode.Unknown && typeName != "unknown")
                throw new AccumulateEncodingException($"Unknown transaction type: {typeName}");

            using var m = new Marshaller();
            m.WriteUInt(1, typeCode);

            switch (typeCode)
            {
                case TransactionTypeCode.SendTokens:
                    MarshalSendTokens(m, body);
                    break;
                case TransactionTypeCode.CreateIdentity:
                    MarshalCreateIdentity(m, body);
                    break;
                case TransactionTypeCode.CreateTokenAccount:
                    MarshalCreateTokenAccount(m, body);
                    break;
                case TransactionTypeCode.CreateDataAccount:
                    MarshalCreateDataAccount(m, body);
                    break;
                case TransactionTypeCode.WriteData:
                    MarshalWriteData(m, body);
                    break;
                case TransactionTypeCode.WriteDataTo:
                    MarshalWriteDataTo(m, body);
                    break;
                case TransactionTypeCode.AcmeFaucet:
                    MarshalAcmeFaucet(m, body);
                    break;
                case TransactionTypeCode.CreateToken:
                    MarshalCreateToken(m, body);
                    break;
                case TransactionTypeCode.IssueTokens:
                    MarshalIssueTokens(m, body);
                    break;
                case TransactionTypeCode.BurnTokens:
                    MarshalBurnTokens(m, body);
                    break;
                case TransactionTypeCode.CreateKeyPage:
                    MarshalCreateKeyPage(m, body);
                    break;
                case TransactionTypeCode.CreateKeyBook:
                    MarshalCreateKeyBook(m, body);
                    break;
                case TransactionTypeCode.AddCredits:
                    MarshalAddCredits(m, body);
                    break;
                case TransactionTypeCode.UpdateKeyPage:
                    MarshalUpdateKeyPage(m, body);
                    break;
                case TransactionTypeCode.UpdateAccountAuth:
                    MarshalUpdateAccountAuth(m, body);
                    break;
                case TransactionTypeCode.UpdateKey:
                    MarshalUpdateKey(m, body);
                    break;
                case TransactionTypeCode.LockAccount:
                    MarshalLockAccount(m, body);
                    break;
                case TransactionTypeCode.TransferCredits:
                    MarshalTransferCredits(m, body);
                    break;
                case TransactionTypeCode.BurnCredits:
                    MarshalBurnCredits(m, body);
                    break;
                default:
                    // For unknown types, just write the type code (already written)
                    break;
            }

            return m.GetBytes();
        }

        /// <summary>
        /// Marshal signature metadata to binary TLV. Field numbers MUST match Go core's generated
        /// marshaler for ED25519Signature (types_gen.go): the metadata is the signature with the
        /// Signature (tag 3) and TransactionHash (tag 8) fields cleared.
        ///   1=Type, 2=PublicKey, 4=Signer, 5=SignerVersion, 6=Timestamp, 7=Vote, 9=Memo, 10=Data
        /// (NOTE: Memo/Data were previously emitted at 10/11 — an off-by-one that produced an
        ///  invalid metadata hash whenever a signature memo/data was set. Fixed to 9/10.)
        /// </summary>
        internal static byte[] MarshalSignatureMetadata(
            int signatureType,
            byte[] publicKey,
            string signerUrl,
            int signerVersion,
            long timestamp,
            int vote,
            string? memo,
            byte[]? data)
        {
            using var m = new Marshaller();

            m.WriteUInt(1, signatureType);           // tag 01: type (uvarint)
            m.WriteBytes(2, publicKey);               // tag 02: publicKey (bytes)
            m.WriteUrl(4, new Url(signerUrl));        // tag 04: signer (URL)
            m.WriteUInt(5, (long)signerVersion);      // tag 05: signerVersion (uvarint)
            m.WriteUVarint(6, (ulong)timestamp);      // tag 06: timestamp (uvarint)

            if (vote != 0)
                m.WriteUInt(7, vote);                 // tag 07: vote (uvarint)

            if (!string.IsNullOrEmpty(memo))
                m.WriteString(9, memo);               // tag 09: memo (string)

            if (data != null && data.Length > 0)
                m.WriteBytes(10, data);               // tag 10: data (bytes)

            return m.GetBytes();
        }

        // ---- Per-type body marshallers ----

        private static void MarshalSendTokens(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 04 (repeated): recipients
            var to = GetListValue(body, "to");
            if (to != null)
            {
                foreach (var recipient in to)
                {
                    var recipientDict = ToDictionary(recipient);
                    if (recipientDict != null)
                    {
                        using var rm = new Marshaller();
                        var url = GetStringValue(recipientDict, "url");
                        if (url != null) rm.WriteUrl(1, new Url(url));
                        var amount = GetStringValue(recipientDict, "amount");
                        if (amount != null)
                        {
                            var bigAmount = BigInteger.Parse(amount);
                            var amountBytes = bigAmount.Sign == 0
                                ? Array.Empty<byte>()
                                : bigAmount.ToByteArray(isUnsigned: true, isBigEndian: true);
                            rm.WriteBytes(2, amountBytes);
                        }
                        m.WriteBytes(4, rm.GetBytes());
                    }
                }
            }
        }

        private static void MarshalCreateIdentity(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: url
            var url = GetStringValue(body, "url");
            if (url != null) m.WriteUrl(2, new Url(url));

            // tag 03: keyHash (bytes, length-prefixed — NOT hash encoding)
            var keyHash = GetStringValue(body, "keyHash");
            if (keyHash != null) m.WriteBytes(3, Convert.FromHexString(keyHash));

            // tag 04: keyBookUrl
            var keyBookUrl = GetStringValue(body, "keyBookUrl");
            if (keyBookUrl != null) m.WriteUrl(4, new Url(keyBookUrl));

            // tag 06: authorities (repeated)
            WriteAuthorities(m, body, 6);
        }

        private static void MarshalCreateTokenAccount(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: url
            var url = GetStringValue(body, "url");
            if (url != null) m.WriteUrl(2, new Url(url));

            // tag 03: tokenUrl
            var tokenUrl = GetStringValue(body, "tokenUrl");
            if (tokenUrl != null) m.WriteUrl(3, new Url(tokenUrl));

            // tag 05: authorities (repeated)
            WriteAuthorities(m, body, 5);
        }

        private static void MarshalCreateDataAccount(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: url
            var url = GetStringValue(body, "url");
            if (url != null) m.WriteUrl(2, new Url(url));

            // tag 03: authorities (repeated)
            WriteAuthorities(m, body, 3);
        }

        private static void MarshalWriteData(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: entry
            MarshalDataEntry(m, body, 2);

            // tag 03: scratch
            if (body.TryGetValue("scratch", out var scratch) && scratch is bool scratchBool && scratchBool)
                m.WriteBool(3, true);
        }

        private static void MarshalWriteDataTo(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: recipient
            var recipient = GetStringValue(body, "recipient");
            if (recipient != null) m.WriteUrl(2, new Url(recipient));

            // tag 03: entry
            MarshalDataEntry(m, body, 3);
        }

        private static void MarshalDataEntry(Marshaller m, Dictionary<string, object?> body, int fieldNr)
        {
            var entry = GetDictValue(body, "entry");
            if (entry != null)
            {
                using var em = new Marshaller();

                // Data entry type codes (matching Go protocol):
                //   Factom = 1, Accumulate = 2, DoubleHash = 3
                var entryType = GetStringValue(entry, "type");
                int entryTypeCode = entryType switch
                {
                    "factom" => 1,
                    "accumulate" => 2,
                    "doubleHash" => 3,
                    _ => 2, // default to accumulate
                };
                em.WriteUInt(1, entryTypeCode);

                var data = GetListValue(entry, "data");
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        var hex = item?.ToString();
                        if (hex != null) em.WriteBytes(2, Convert.FromHexString(hex));
                    }
                }

                m.WriteBytes(fieldNr, em.GetBytes());
            }
        }

        private static void MarshalAcmeFaucet(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: url
            var url = GetStringValue(body, "url");
            if (url != null) m.WriteUrl(2, new Url(url));
        }

        private static void MarshalCreateToken(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: url
            var url = GetStringValue(body, "url");
            if (url != null) m.WriteUrl(2, new Url(url));

            // tag 04: symbol (field 3 skipped in Go protocol)
            var symbol = GetStringValue(body, "symbol");
            if (symbol != null) m.WriteString(4, symbol);

            // tag 05: precision
            if (body.TryGetValue("precision", out var precision) && precision != null)
                m.WriteUInt(5, Convert.ToInt32(precision));

            // tag 07: supplyLimit (field 6 is properties URL)
            var supplyLimit = GetStringValue(body, "supplyLimit");
            if (supplyLimit != null)
            {
                var bigLimit = BigInteger.Parse(supplyLimit);
                var limitBytes = bigLimit.Sign == 0
                    ? Array.Empty<byte>()
                    : bigLimit.ToByteArray(isUnsigned: true, isBigEndian: true);
                m.WriteBytes(7, limitBytes);
            }

            // tag 09: authorities (repeated)
            WriteAuthorities(m, body, 9);
        }

        private static void MarshalIssueTokens(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: recipient
            var recipient = GetStringValue(body, "recipient");
            if (recipient != null) m.WriteUrl(2, new Url(recipient));

            // tag 03: amount
            var amount = GetStringValue(body, "amount");
            if (amount != null)
            {
                var bigAmount = BigInteger.Parse(amount);
                var amountBytes = bigAmount.Sign == 0
                    ? Array.Empty<byte>()
                    : bigAmount.ToByteArray(isUnsigned: true, isBigEndian: true);
                m.WriteBytes(3, amountBytes);
            }
        }

        private static void MarshalBurnTokens(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: amount
            var amount = GetStringValue(body, "amount");
            if (amount != null)
            {
                var bigAmount = BigInteger.Parse(amount);
                var amountBytes = bigAmount.Sign == 0
                    ? Array.Empty<byte>()
                    : bigAmount.ToByteArray(isUnsigned: true, isBigEndian: true);
                m.WriteBytes(2, amountBytes);
            }
        }

        private static void MarshalCreateKeyPage(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02 (repeated): keys
            var keys = GetListValue(body, "keys");
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    var keyDict = ToDictionary(key);
                    if (keyDict != null)
                    {
                        using var km = new Marshaller();
                        var keyHash = GetStringValue(keyDict, "keyHash");
                        if (keyHash != null) km.WriteBytes(1, Convert.FromHexString(keyHash));
                        var del = GetStringValue(keyDict, "delegate");
                        if (del != null) km.WriteUrl(2, new Url(del));
                        m.WriteBytes(2, km.GetBytes());
                    }
                }
            }
        }

        private static void MarshalCreateKeyBook(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: url
            var url = GetStringValue(body, "url");
            if (url != null) m.WriteUrl(2, new Url(url));

            // tag 03: publicKeyHash (bytes, length-prefixed — NOT hash encoding)
            var publicKeyHash = GetStringValue(body, "publicKeyHash");
            if (publicKeyHash != null) m.WriteBytes(3, Convert.FromHexString(publicKeyHash));
        }

        private static void MarshalAddCredits(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: recipient
            var recipient = GetStringValue(body, "recipient");
            if (recipient != null) m.WriteUrl(2, new Url(recipient));

            // tag 03: amount
            var amount = GetStringValue(body, "amount");
            if (amount != null)
            {
                var bigAmount = BigInteger.Parse(amount);
                var amountBytes = bigAmount.Sign == 0
                    ? Array.Empty<byte>()
                    : bigAmount.ToByteArray(isUnsigned: true, isBigEndian: true);
                m.WriteBytes(3, amountBytes);
            }

            // tag 04: oracle
            if (body.TryGetValue("oracle", out var oracle) && oracle != null)
                m.WriteUInt(4, Convert.ToInt64(oracle));
        }

        private static void MarshalUpdateKeyPage(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02 (repeated): operation
            var operations = GetListValue(body, "operation");
            if (operations != null)
            {
                foreach (var op in operations)
                {
                    var opDict = ToDictionary(op);
                    if (opDict == null) continue;

                    using var om = new Marshaller();
                    var opType = GetStringValue(opDict, "type");
                    int opTypeCode = opType switch
                    {
                        "add" => 3,
                        "remove" => 1,
                        "update" => 2,
                        "setThreshold" => 4,
                        "updateAllowed" => 5,
                        "setRejectThreshold" => 6,
                        "setResponseThreshold" => 7,
                        _ => 0,
                    };
                    om.WriteUInt(1, opTypeCode);

                    if (opDict.TryGetValue("entry", out var entry))
                    {
                        var entryDict = ToDictionary(entry);
                        if (entryDict != null)
                        {
                            using var em = new Marshaller();
                            var keyHash = GetStringValue(entryDict, "keyHash");
                            if (keyHash != null) em.WriteBytes(1, Convert.FromHexString(keyHash));
                            var del = GetStringValue(entryDict, "delegate");
                            if (del != null) em.WriteUrl(2, new Url(del));
                            om.WriteBytes(2, em.GetBytes());
                        }
                    }

                    if (opDict.TryGetValue("threshold", out var threshold) && threshold != null)
                        om.WriteUInt(2, Convert.ToInt32(threshold));

                    m.WriteBytes(2, om.GetBytes());
                }
            }
        }

        private static void MarshalUpdateAccountAuth(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02 (repeated): operations
            var operations = GetListValue(body, "operations");
            if (operations != null)
            {
                foreach (var op in operations)
                {
                    var opDict = ToDictionary(op);
                    if (opDict == null) continue;

                    using var om = new Marshaller();
                    var opType = GetStringValue(opDict, "type");
                    int opTypeCode = opType switch
                    {
                        "enable" => 1,
                        "disable" => 2,
                        "addAuthority" => 3,
                        "removeAuthority" => 4,
                        _ => 0,
                    };
                    om.WriteUInt(1, opTypeCode);
                    var authorityUrl = GetStringValue(opDict, "authority");
                    if (authorityUrl != null) om.WriteUrl(2, new Url(authorityUrl));
                    m.WriteBytes(2, om.GetBytes());
                }
            }
        }

        private static void MarshalLockAccount(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: height
            if (body.TryGetValue("height", out var height) && height != null)
                m.WriteUInt(2, Convert.ToInt64(height));
        }

        private static void MarshalBurnCredits(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: amount
            if (body.TryGetValue("amount", out var amount) && amount != null)
                m.WriteUInt(2, Convert.ToInt64(amount));
        }

        private static void MarshalUpdateKey(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02: newKeyHash (bytes)
            var newKeyHash = GetStringValue(body, "newKeyHash");
            if (newKeyHash != null) m.WriteBytes(2, Convert.FromHexString(newKeyHash));
        }

        private static void MarshalTransferCredits(Marshaller m, Dictionary<string, object?> body)
        {
            // tag 02 (repeated): CreditRecipient { url, amount }
            var to = GetListValue(body, "to");
            if (to != null)
            {
                foreach (var recipient in to)
                {
                    var recipientDict = ToDictionary(recipient);
                    if (recipientDict != null)
                    {
                        using var rm = new Marshaller();
                        var url = GetStringValue(recipientDict, "url");
                        if (url != null) rm.WriteUrl(1, new Url(url));
                        if (recipientDict.TryGetValue("amount", out var amount) && amount != null)
                            rm.WriteUInt(2, Convert.ToInt64(amount));
                        m.WriteBytes(2, rm.GetBytes());
                    }
                }
            }
        }

        // ---- Helpers ----

        private static void WriteAuthorities(Marshaller m, Dictionary<string, object?> body, int fieldNr)
        {
            var authorities = GetListValue(body, "authorities");
            if (authorities != null)
            {
                foreach (var auth in authorities)
                {
                    // Authorities are plain URL strings. The nested { "url": ... }
                    // form is still accepted so bodies built by older callers keep
                    // hashing to the same bytes.
                    string? authUrl = auth as string;
                    if (authUrl == null)
                    {
                        var authDict = ToDictionary(auth);
                        if (authDict != null) authUrl = GetStringValue(authDict, "url");
                    }
                    if (!string.IsNullOrEmpty(authUrl)) m.WriteUrl(fieldNr, new Url(authUrl!));
                }
            }
        }

        private static string? GetStringValue(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is string s) return s;
                if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                    return je.GetString();
                return value?.ToString();
            }
            return null;
        }

        private static List<object?>? GetListValue(Dictionary<string, object?> dict, string key)
        {
            if (!dict.TryGetValue(key, out var value) || value == null) return null;

            if (value is List<object?> list) return list;
            if (value is List<object> listObj) return listObj.Cast<object?>().ToList();
            if (value is IEnumerable<object> enumerable) return enumerable.Cast<object?>().ToList();
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                var result = new List<object?>();
                foreach (var item in je.EnumerateArray())
                    result.Add(item);
                return result;
            }
            return null;
        }

        private static Dictionary<string, object?>? GetDictValue(Dictionary<string, object?> dict, string key)
        {
            if (!dict.TryGetValue(key, out var value) || value == null) return null;
            return ToDictionary(value);
        }

        private static Dictionary<string, object?>? ToDictionary(object? value)
        {
            if (value is Dictionary<string, object?> d) return d;
            if (value is Dictionary<string, object> d2)
                return d2.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                var result = new Dictionary<string, object?>();
                foreach (var prop in je.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetInt64(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => prop.Value
                    };
                }
                return result;
            }
            return null;
        }
    }
}
