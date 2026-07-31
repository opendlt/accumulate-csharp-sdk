using Acme.Net.Sdk.Protocol;

namespace Acme.Net.Sdk.Transactions
{
    /// <summary>
    /// A recipient for TxBody.SendTokens (JSON wire format).
    /// Named TxRecipient to avoid collision with Protocol.Generated.Protocol.TokenRecipient.
    /// </summary>
    public class TxRecipient
    {
        public string Url { get; set; } = "";
        public string Amount { get; set; } = "0";

        public TxRecipient() { }

        public TxRecipient(string url, string amount)
        {
            Url = url;
            Amount = amount;
        }

        public Dictionary<string, object> ToDict() => new()
        {
            ["url"] = Url,
            ["amount"] = Amount
        };
    }

    /// <summary>
    /// Parameters for a key specification entry in a key page.
    /// </summary>
    public class KeySpecParams
    {
        public string KeyHash { get; set; } = "";
        public string? Delegate { get; set; }

        public KeySpecParams() { }

        public KeySpecParams(string keyHash, string? @delegate = null)
        {
            KeyHash = keyHash;
            Delegate = @delegate;
        }

        public Dictionary<string, object?> ToDict()
        {
            var d = new Dictionary<string, object?> { ["keyHash"] = KeyHash };
            if (Delegate != null) d["delegate"] = Delegate;
            return d;
        }
    }

    /// <summary>
    /// Static factory methods for building transaction body dictionaries matching
    /// the Accumulate JSON wire format. Mirrors Dart builders.dart and Python convenience.py.
    /// Each method returns a Dictionary with a "type" field matching the camelCase wire name.
    /// </summary>
    public static class TxBody
    {
        // ---- Identity ----

        public static Dictionary<string, object?> CreateIdentity(
            string url, string keyBookUrl, string publicKeyHash, List<string>? authorities = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = "createIdentity",
                ["url"] = url,
                ["keyBookUrl"] = keyBookUrl,
                ["keyHash"] = publicKeyHash,
            };
            // The protocol field is a repeated URL, so it serialises as a plain
            // array of strings. Wrapping each entry in { "url": ... } produces
            // JSON the node cannot unmarshal, even though the binary hash matches.
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = new List<string>(authorities);
            return body;
        }

        /// <summary>
        /// Builds a <c>createIdentity</c> body for a SUB-ADI that does NOT create its own key book.
        /// With no key book and no explicit authorities, the sub-ADI is created with an empty
        /// authority set and INHERITS authority from its parent identity — the Accumulate executor
        /// resolves the controlling authority by walking up the identity chain
        /// (<c>internal/core/block/shared/shared.go:GetAccountAuthoritySet</c> →
        /// <c>execute/v2/block/utils.go:getAccountAuthoritySet</c>, which recurses to the parent
        /// when an account's authority set is empty). The parent's key page therefore signs and
        /// pays for this sub-ADI and anything created under it — no new keys or credits required.
        /// <para>
        /// Only valid for sub-ADIs: a root identity must be created with a key book or explicit
        /// authorities (Go core <c>create_identity.go</c> rejects an empty authority set on a root).
        /// The transaction principal must be the immediate parent identity.
        /// </para>
        /// </summary>
        public static Dictionary<string, object?> CreateIdentityInherited(string url, List<string>? authorities = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = "createIdentity",
                ["url"] = url,
            };
            // The protocol field is a repeated URL, so it serialises as a plain
            // array of strings. Wrapping each entry in { "url": ... } produces
            // JSON the node cannot unmarshal, even though the binary hash matches.
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = new List<string>(authorities);
            return body;
        }

        // ---- Tokens ----

        public static Dictionary<string, object?> SendTokens(List<TxRecipient> to)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "sendTokens",
                ["to"] = to.Select(r => r.ToDict()).ToList<object>(),
            };
        }

        public static Dictionary<string, object?> SendTokensSingle(string toUrl, string amount)
        {
            return SendTokens(new List<TxRecipient> { new(toUrl, amount) });
        }

        public static Dictionary<string, object?> IssueTokens(string recipient, string amount)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "issueTokens",
                ["recipient"] = recipient,
                ["amount"] = amount,
            };
        }

        public static Dictionary<string, object?> BurnTokens(string amount)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "burnTokens",
                ["amount"] = amount,
            };
        }

        public static Dictionary<string, object?> CreateTokenAccount(
            string url, string tokenUrl = "acc://ACME", List<string>? authorities = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = "createTokenAccount",
                ["url"] = url,
                ["tokenUrl"] = tokenUrl,
            };
            // The protocol field is a repeated URL, so it serialises as a plain
            // array of strings. Wrapping each entry in { "url": ... } produces
            // JSON the node cannot unmarshal, even though the binary hash matches.
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = new List<string>(authorities);
            return body;
        }

        public static Dictionary<string, object?> CreateToken(
            string url, string symbol, int precision, string? supplyLimit = null, List<string>? authorities = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = "createToken",
                ["url"] = url,
                ["symbol"] = symbol,
                ["precision"] = precision,
            };
            if (supplyLimit != null) body["supplyLimit"] = supplyLimit;
            // The protocol field is a repeated URL, so it serialises as a plain
            // array of strings. Wrapping each entry in { "url": ... } produces
            // JSON the node cannot unmarshal, even though the binary hash matches.
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = new List<string>(authorities);
            return body;
        }

        // ---- Credits ----

        public static Dictionary<string, object?> AddCredits(string recipient, string amount, int oracle)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "addCredits",
                ["recipient"] = recipient,
                ["amount"] = amount,
                ["oracle"] = oracle,
            };
        }

        public static Dictionary<string, object?> TransferCredits(string toUrl, int amount)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "transferCredits",
                ["to"] = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["url"] = toUrl, ["amount"] = amount }
                },
            };
        }

        public static Dictionary<string, object?> BurnCredits(int amount)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "burnCredits",
                ["amount"] = amount,
            };
        }

        // ---- Data ----

        public static Dictionary<string, object?> CreateDataAccount(string url, List<string>? authorities = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = "createDataAccount",
                ["url"] = url,
            };
            // The protocol field is a repeated URL, so it serialises as a plain
            // array of strings. Wrapping each entry in { "url": ... } produces
            // JSON the node cannot unmarshal, even though the binary hash matches.
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = new List<string>(authorities);
            return body;
        }

        public static Dictionary<string, object?> WriteData(List<string> entriesHex, bool scratch = false)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = "writeData",
                ["entry"] = new Dictionary<string, object?>
                {
                    ["type"] = "doubleHash",
                    ["data"] = entriesHex,
                },
            };
            if (scratch) body["scratch"] = true;
            return body;
        }

        public static Dictionary<string, object?> WriteDataTo(string recipient, List<string> entriesHex)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "writeDataTo",
                ["recipient"] = recipient,
                ["entry"] = new Dictionary<string, object?>
                {
                    ["type"] = "doubleHash",
                    ["data"] = entriesHex,
                },
            };
        }

        // ---- Key Management ----

        public static Dictionary<string, object?> CreateKeyBook(string url, string publicKeyHash)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "createKeyBook",
                ["url"] = url,
                ["publicKeyHash"] = publicKeyHash,
            };
        }

        public static Dictionary<string, object?> CreateKeyPage(List<KeySpecParams> keys)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "createKeyPage",
                ["keys"] = keys.Select(k => k.ToDict()).ToList<object?>(),
            };
        }

        public static Dictionary<string, object?> UpdateKeyPage(List<Dictionary<string, object?>> operations)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "updateKeyPage",
                ["operation"] = operations,
            };
        }

        // Key page operation helpers

        public static Dictionary<string, object?> AddKeyOperation(string keyHash)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "add",
                ["entry"] = new Dictionary<string, object?> { ["keyHash"] = keyHash },
            };
        }

        public static Dictionary<string, object?> RemoveKeyOperation(string keyHash)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "remove",
                ["entry"] = new Dictionary<string, object?> { ["keyHash"] = keyHash },
            };
        }

        public static Dictionary<string, object?> SetThresholdOperation(int threshold)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "setThreshold",
                ["threshold"] = threshold,
            };
        }

        public static Dictionary<string, object?> UpdateKey(string newKeyHash)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "updateKey",
                ["newKeyHash"] = newKeyHash,
            };
        }

        // ---- Account Auth ----

        public static Dictionary<string, object?> UpdateAccountAuth(List<Dictionary<string, object?>> operations)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "updateAccountAuth",
                ["operations"] = operations,
            };
        }

        public static Dictionary<string, object?> LockAccount(int height)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "lockAccount",
                ["height"] = height,
            };
        }

        // ---- Remote (sign-pending) ----

        /// <summary>
        /// Builds a <c>remote</c> transaction body that references an already-existing transaction
        /// by its hash. This is the body used to add a signature to a PENDING transaction (the
        /// "sign pending" flow) without re-supplying the original transaction's full body.
        /// <para>
        /// Matches Go core: <c>RemoteTransaction</c> whose <c>GetHash()</c> returns the embedded
        /// hash directly (see <c>protocol/transaction_hash.go</c> <c>calcHash</c>), so a co-signer
        /// signs the SAME transaction hash the initiator signed.
        /// </para>
        /// </summary>
        /// <param name="transactionHashHex">Lower-case hex of the 32-byte transaction hash to sign.</param>
        public static Dictionary<string, object?> Remote(string transactionHashHex)
        {
            if (string.IsNullOrEmpty(transactionHashHex))
                throw new ArgumentException("transactionHashHex is required", nameof(transactionHashHex));
            return new Dictionary<string, object?>
            {
                ["type"] = "remote",
                ["hash"] = transactionHashHex,
            };
        }

        // ---- Other ----

        public static Dictionary<string, object?> AcmeFaucet(string url)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "acmeFaucet",
                ["url"] = url,
            };
        }
    }
}
