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
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = authorities.Select(a => new Dictionary<string, object?> { ["url"] = a }).ToList();
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
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = authorities.Select(a => new Dictionary<string, object?> { ["url"] = a }).ToList();
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
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = authorities.Select(a => new Dictionary<string, object?> { ["url"] = a }).ToList();
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
            if (authorities != null && authorities.Count > 0)
                body["authorities"] = authorities.Select(a => new Dictionary<string, object?> { ["url"] = a }).ToList();
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
