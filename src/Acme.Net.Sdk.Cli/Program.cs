// Accumulate SDK command-line interface (RB-04).
//
// Contract: docs/ai-agent-readiness/CLI-SPEC.md in accumulate-studio.
//   * Under --json, stdout carries EXACTLY ONE envelope object. Logs go to stderr.
//   * Exit codes: 0 ok · 1 operation failed · 2 usage error · 3 network unreachable.
//   * Errors carry canonical ACC_* codes so `retryable` tells an agent whether a
//     retry is productive instead of leaving it to guess.
//   * Never prompts. Mainnet needs --network mainnet AND ACCUMULATE_ALLOW_MAINNET=1.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Acme.Net.Sdk.Cli;

internal sealed record CatalogEntry(
    string Category,
    bool Retryable,
    long[] ProtocolCodes,
    string[] Patterns,
    string Hint,
    string Remediation);

internal sealed record VerbArg(string Name, string Type, bool Required);

internal sealed record VerbFlag(
    string Name,
    string Type,
    bool Required,
    string? Default = null,
    bool Repeatable = false);

internal sealed record VerbSpec(
    string Name,
    string Summary,
    bool Network,
    bool Signs,
    VerbArg[] Args,
    VerbFlag[] Flags);

internal sealed class UsageException : Exception
{
    public UsageException(string message) : base(message) { }
}

internal static class Program
{
    private const string EnvelopeVersion = "1";
    private const string SdkName = "csharp";
    /// Read from the assembly rather than hardcoded: a literal here silently ships
    /// a stale version the moment the csproj is bumped.
    private static readonly string SdkVersion =
        typeof(Program).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion?.Split('+')[0]
        ?? typeof(Program).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";

    private const int ExitOk = 0;
    private const int ExitFailed = 1;
    private const int ExitUsage = 2;
    private const int ExitNetwork = 3;

    private const string DefaultNetwork = "kermit";

    /// Mirrors packages/codegen/src/manifests/errors.catalog.json. Wire codes were
    /// verified against a live node by tools/agent-harness/negative-cases.mjs.
    private static readonly Dictionary<string, CatalogEntry> Catalog = new()
    {
        ["ACC_ACCOUNT_NOT_FOUND"] = new("not_found", false, new long[] { -32807, -33404 },
            new[] { "accumulate error not found", "not found", "-32807", "-33404" },
            "The account URL does not exist on this network.",
            "Verify the URL and the network. If you just created the account, wait for its creating transaction to reach 'delivered' first. Note that on the V2 API a malformed URL is also reported as not-found."),
        ["ACC_INVALID_PARAMS"] = new("validation", false, new long[] { -32802, -32602 },
            new[] { "validation error", "field validation for", "invalid params", "-32802", "-32602" },
            "The request parameters were rejected by the node.",
            "Check the operation's declared inputs. Hashes are 32-byte hex; amounts are base-unit integers."),
        ["ACC_METHOD_NOT_FOUND"] = new("validation", false, new long[] { -32601 },
            new[] { "method not found", "-32601" },
            "The node does not expose the RPC method that was called.",
            "Use the SDK's canonical client rather than raw RPC; it targets the right API version."),
        ["ACC_ROUTING_FAILED"] = new("validation", false, new long[] { -33400 },
            new[] { "cannot route request", "nothing to route", "scope is missing", "-33400" },
            "The node could not determine which partition should handle the request.",
            "Every transaction needs a header with a valid `principal` — that URL is the routing key. Build envelopes with TxBody + SmartSigner rather than by hand."),
        ["ACC_INSUFFICIENT_CREDITS"] = new("insufficient_credits", false, Array.Empty<long>(),
            new[] { "insufficientcredits", "insufficient credits" },
            "The signing key page does not hold enough credits to pay for this transaction.",
            "Call add_credits for the SIGNING key page, then wait for the credits to settle."),
        ["ACC_UNAUTHORIZED_SIGNER"] = new("auth", false, new long[] { 403 },
            new[] { "unauthorized", "key does not belong to signer" },
            "The signing key is not on the key page that authorizes this principal.",
            "Sign with a key on the principal's authorizing key page (after create_identity, `<adi>/book/1`)."),
        ["ACC_INSUFFICIENT_BALANCE"] = new("insufficient_balance", false, Array.Empty<long>(),
            new[] { "insufficient balance", "insufficient funds", "exceeds balance" },
            "The source account does not hold enough tokens for this transfer.",
            "Confirm the balance first. 1 ACME = 1e8 base units; custom tokens carry their own precision."),
        ["ACC_NETWORK_UNAVAILABLE"] = new("network", true, Array.Empty<long>(),
            new[]
            {
                "econnrefused", "econnreset", "etimedout", "timeout", "connection closed",
                "connection reset", "connection refused", "service unavailable",
                "no such host", "actively refused", "httprequestexception",
                "an error occurred while sending the request", "taskcanceledexception",
            },
            "The endpoint could not be reached, or the request timed out.",
            "Retry with exponential backoff. This is the only class where a bare retry is productive."),
        ["ACC_INTERNAL"] = new("internal", true, new long[] { -32603 },
            new[] { "internal error", "-32603" },
            "The node reported an internal error.",
            "Retry once with backoff. If it persists, re-check the request shape."),
        ["ACC_USAGE"] = new("validation", false, Array.Empty<long>(), Array.Empty<string>(),
            "The command was invoked incorrectly.",
            "Run `accumulate --help --json` for the full command tree, flags and required arguments."),
    };

    private static readonly VerbSpec[] Verbs =
    {
        new("query", "Query any Accumulate account", true, false,
            new[] { new VerbArg("url", "string", true) }, Array.Empty<VerbFlag>()),
        new("balance", "Get a token account balance", true, false,
            new[] { new VerbArg("url", "string", true) }, Array.Empty<VerbFlag>()),
        new("chain", "Read chain entries for an account", true, false,
            new[] { new VerbArg("url", "string", true) },
            new[]
            {
                new VerbFlag("--chain", "string", false, "main"),
                new VerbFlag("--start", "integer", false, "0"),
                new VerbFlag("--count", "integer", false, "10"),
            }),
        new("faucet", "Request testnet ACME for a lite token account", true, false,
            new[] { new VerbArg("url", "string", true) }, Array.Empty<VerbFlag>()),
        new("credits estimate", "Estimate credits purchased for an ACME amount", true, false,
            new[] { new VerbArg("url", "string", true) },
            new[] { new VerbFlag("--amount", "number", true) }),
        new("tx build", "Build an unsigned transaction body", false, false,
            new[] { new VerbArg("op", "string", true) },
            new[]
            {
                new VerbFlag("--param", "key=value", false, null, true),
                new VerbFlag("--out", "path", false),
            }),
        new("tx sign", "Sign a body into an envelope, or co-sign an existing envelope (M-of-N)",
            true, true,
            Array.Empty<VerbArg>(),
            new[]
            {
                new VerbFlag("--body", "path", false),
                new VerbFlag("--envelope", "path", false),
                new VerbFlag("--principal", "string", false),
                new VerbFlag("--signer", "string", true),
                new VerbFlag("--key-file", "path", false),
                new VerbFlag("--key-env", "string", false),
                new VerbFlag("--out", "path", false),
            }),
        new("tx submit", "Submit an ALREADY-SIGNED envelope (does not sign)", true, false,
            Array.Empty<VerbArg>(),
            new[] { new VerbFlag("--envelope", "path", true) }),
        new("tx wait", "Poll a transaction until it reaches a final state", true, false,
            new[] { new VerbArg("txid", "string", true) },
            new[] { new VerbFlag("--timeout", "integer", false, "60") }),
        new("tx status", "Read a transaction's current status", true, false,
            new[] { new VerbArg("txid", "string", true) }, Array.Empty<VerbFlag>()),
        new("keys generate", "Generate a keypair (never written to disk)", false, false,
            Array.Empty<VerbArg>(),
            new[] { new VerbFlag("--algorithm", "string", false, "ed25519") }),
        new("net list", "List known networks", false, false,
            Array.Empty<VerbArg>(), Array.Empty<VerbFlag>()),
        new("net status", "Check the selected network's reachability", true, false,
            Array.Empty<VerbArg>(), Array.Empty<VerbFlag>()),
        new("version", "Report SDK and envelope versions", false, false,
            Array.Empty<VerbArg>(), Array.Empty<VerbFlag>()),
    };

    private static readonly HashSet<string> Groups = new() { "credits", "tx", "keys", "net" };

    private static readonly Dictionary<string, string> Endpoints = new()
    {
        ["kermit"] = "https://kermit.accumulatenetwork.io",
        ["testnet"] = "https://testnet.accumulatenetwork.io",
        ["mainnet"] = "https://mainnet.accumulatenetwork.io",
        ["local"] = "http://localhost:26660",
    };

    private static bool _asJson;
    private static string? _network;

    /// The real stdout, captured before the SDK can redirect or write to it.
    ///
    /// stdout is protocol here: the envelope must be the only thing on it. The
    /// SDK prints diagnostics (e.g. "[SignatureKeyPair] Imported Ed25519 seed")
    /// straight to Console.Out mid-call, which corrupted the envelope. Console.Out
    /// is swapped for a buffer in Main and everything the SDK writes is forwarded
    /// to stderr, so no future SDK chatter can break the contract either.
    /// Assigned explicitly at the top of Main, BEFORE Console.Out is swapped.
    /// A `static readonly ... = Console.Out` initializer is evaluated lazily on
    /// first access, which happens after the swap — so it captured the buffer and
    /// the envelope vanished.
    private static TextWriter RealOut = Console.Out;
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static bool _emitted;

    /// Longest pattern wins, so "key does not belong to signer" beats bare "unauthorized".
    ///
    /// Unrecognized errors deliberately fall back to a NON-retryable code: unknown
    /// failures are far more often malformed requests than transient faults, and
    /// defaulting to retryable is how an agent burns its turn budget in a loop.
    private static string Classify(string raw)
    {
        var text = (raw ?? string.Empty).ToLowerInvariant();
        string? best = null;
        var bestLen = -1;
        foreach (var (code, e) in Catalog)
        {
            foreach (var p in e.Patterns)
            {
                if (text.Contains(p, StringComparison.Ordinal) && p.Length > bestLen)
                {
                    best = code;
                    bestLen = p.Length;
                }
            }
        }
        return best ?? "ACC_INVALID_PARAMS";
    }

    private static JsonObject Meta() => new()
    {
        ["network"] = _network,
        ["sdk"] = SdkName,
        ["version"] = SdkVersion,
        ["durationMs"] = Clock.ElapsedMilliseconds,
    };

    private static int Ok(JsonNode? data)
    {
        Debug.Assert(!_emitted, "envelope emitted twice");
        _emitted = true;
        if (_asJson)
        {
            var env = new JsonObject
            {
                ["envelope"] = EnvelopeVersion,
                ["ok"] = true,
                ["data"] = data ?? new JsonObject(),
                ["meta"] = Meta(),
            };
            RealOut.WriteLine(env.ToJsonString());
        }
        else
        {
            RealOut.WriteLine((data ?? new JsonObject()).ToJsonString(
                new JsonSerializerOptions { WriteIndented = true }));
        }
        return ExitOk;
    }

    private static int Fail(string raw, string? code = null, int? exitCode = null)
    {
        Debug.Assert(!_emitted, "envelope emitted twice");
        _emitted = true;
        var resolved = code ?? Classify(raw);
        var e = Catalog[resolved];
        var error = new JsonObject
        {
            ["code"] = resolved,
            ["category"] = e.Category,
            ["retryable"] = e.Retryable,
            ["hint"] = e.Hint,
            ["remediation"] = e.Remediation,
            ["raw"] = raw ?? string.Empty,
        };
        if (e.ProtocolCodes.Length > 0)
        {
            var arr = new JsonArray();
            foreach (var c in e.ProtocolCodes) arr.Add(c);
            error["protocolCodes"] = arr;
        }

        var ec = exitCode ?? resolved switch
        {
            "ACC_USAGE" => ExitUsage,
            "ACC_NETWORK_UNAVAILABLE" => ExitNetwork,
            _ => ExitFailed,
        };

        if (_asJson)
        {
            var env = new JsonObject
            {
                ["envelope"] = EnvelopeVersion,
                ["ok"] = false,
                ["error"] = error,
                ["meta"] = Meta(),
            };
            RealOut.WriteLine(env.ToJsonString());
        }
        else
        {
            Console.Error.WriteLine($"error: {resolved}: {e.Hint}");
            Console.Error.WriteLine($"  retryable: {(e.Retryable ? "yes" : "no")}");
            Console.Error.WriteLine($"  fix: {e.Remediation}");
        }
        return ec;
    }


    /// Case- and underscore-insensitive parameter lookup, so snake_case and
    /// camelCase both work. Op and parameter names differ per SDK, and making an
    /// agent learn each one defeats the point of a single CLI spec.
    private static string? Pick(Dictionary<string, object> a, string name)
    {
        static string N(string x) => x.Replace("_", string.Empty).ToLowerInvariant();
        var target = N(name);
        foreach (var (k, v) in a)
        {
            if (N(k) == target && v is string sv) return sv;
        }
        return null;
    }

    private static string Req(Dictionary<string, object> a, string name, string op) =>
        Pick(a, name) ?? throw new UsageException($"'{op}' requires --param {name}");

    private static int ReqInt(Dictionary<string, object> a, string name, string op)
    {
        var raw = Req(a, name, op);
        if (!int.TryParse(raw, out var v))
            throw new UsageException($"--param {name} must be an integer");
        return v;
    }

    private static readonly string[] BuildOps =
    {
        "create_identity", "create_token_account", "create_data_account", "create_token",
        "send_tokens_single", "issue_tokens", "burn_tokens", "add_credits",
        "transfer_credits", "burn_credits", "write_data", "create_key_book",
    };

    /// Builder dispatch is explicit rather than reflective. Every branch delegates
    /// to TxBody, which is what keeps the produced bytes identical to the SDK path
    /// — these are consensus-visible.
    private static Dictionary<string, object?> BuildBody(string op, Dictionary<string, object> a)
    {
        var n = op.Replace("_", string.Empty).ToLowerInvariant();
        return n switch
        {
            "createidentity" => Acme.Net.Sdk.Transactions.TxBody.CreateIdentity(
                Req(a, "url", op), Req(a, "key_book_url", op), Req(a, "public_key_hash", op)),
            "createtokenaccount" => Acme.Net.Sdk.Transactions.TxBody.CreateTokenAccount(
                Req(a, "url", op), Req(a, "token_url", op)),
            "createdataaccount" => Acme.Net.Sdk.Transactions.TxBody.CreateDataAccount(Req(a, "url", op)),
            "createtoken" => Acme.Net.Sdk.Transactions.TxBody.CreateToken(
                Req(a, "url", op), Req(a, "symbol", op), ReqInt(a, "precision", op)),
            "sendtokenssingle" => Acme.Net.Sdk.Transactions.TxBody.SendTokensSingle(
                Req(a, "to_url", op), Req(a, "amount", op)),
            "issuetokens" => Acme.Net.Sdk.Transactions.TxBody.IssueTokens(
                Req(a, "recipient", op), Req(a, "amount", op)),
            "burntokens" => Acme.Net.Sdk.Transactions.TxBody.BurnTokens(Req(a, "amount", op)),
            "addcredits" => Acme.Net.Sdk.Transactions.TxBody.AddCredits(
                Req(a, "recipient", op), Req(a, "amount", op), ReqInt(a, "oracle", op)),
            "transfercredits" => Acme.Net.Sdk.Transactions.TxBody.TransferCredits(
                Req(a, "to_url", op), ReqInt(a, "amount", op)),
            "burncredits" => Acme.Net.Sdk.Transactions.TxBody.BurnCredits(ReqInt(a, "amount", op)),
            "writedata" => Acme.Net.Sdk.Transactions.TxBody.WriteData(
                new List<string> { Req(a, "data", op) }),
            "createkeybook" => Acme.Net.Sdk.Transactions.TxBody.CreateKeyBook(
                Req(a, "url", op), Req(a, "public_key_hash", op)),
            _ => throw new UsageException(
                $"unknown transaction op '{op}' - available: {string.Join(", ", BuildOps)}"),
        };
    }

    /// Resolve the signing key from an EXPLICIT source only.
    ///
    /// Never falls back to an ambient default: a CLI that quietly finds a key is
    /// a CLI that signs something the caller did not intend. Keys are never
    /// positional either, so they stay out of shell history.
    private static string LoadPrivateKey(Dictionary<string, object> a)
    {
        var keyFile = Str(a, "key_file");
        var keyEnv = Str(a, "key_env");
        if (keyFile is not null && keyEnv is not null)
            throw new UsageException("pass only one of --key-file or --key-env");
        if (keyFile is not null)
        {
            try { return File.ReadAllText(keyFile).Trim(); }
            catch (Exception e) { throw new UsageException($"could not read --key-file: {e.Message}"); }
        }
        if (keyEnv is not null)
        {
            var v = Environment.GetEnvironmentVariable(keyEnv);
            if (string.IsNullOrWhiteSpace(v))
                throw new UsageException($"--key-env '{keyEnv}' is not set or empty");
            return v.Trim();
        }
        throw new UsageException(
            "signing requires an explicit key source: --key-file <path> or --key-env <VAR>. " +
            "No ambient default key is ever used.");
    }


    /// Read an envelope file into the Dictionary/List shape SmartSigner works with.
    ///
    /// System.Text.Json would otherwise hand back JsonElement values, which do not
    /// match the `List&lt;object?&gt;` / `Dictionary&lt;string, object?&gt;` the signer
    /// inspects, so co-signing would fail to find the existing signatures.
    private static Dictionary<string, object?> ReadEnvelope(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new UsageException("envelope is not a JSON object");
        // Accept both the bare envelope and the {"envelope": {...}} submit shape.
        if (node["envelope"] is JsonObject inner) node = inner;
        return (Dictionary<string, object?>)ToClr(node)!;
    }

    private static object? ToClr(JsonNode? n) => n switch
    {
        null => null,
        JsonObject o => o.ToDictionary(kv => kv.Key, kv => ToClr(kv.Value)),
        JsonArray arr => arr.Select(ToClr).ToList(),
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        JsonValue v when v.TryGetValue<bool>(out var b) => b,
        JsonValue v when v.TryGetValue<long>(out var i) => i,
        JsonValue v when v.TryGetValue<double>(out var d) => d,
        _ => n.ToJsonString(),
    };

    private static string BaseUrl(string network)
    {
        if (network == "mainnet" &&
            Environment.GetEnvironmentVariable("ACCUMULATE_ALLOW_MAINNET") != "1")
        {
            throw new UsageException(
                "refusing to target mainnet: pass --network mainnet AND set " +
                "ACCUMULATE_ALLOW_MAINNET=1. Both are required, deliberately.");
        }
        if (!Endpoints.TryGetValue(network, out var ep))
        {
            throw new UsageException(
                $"unknown network '{network}' — known: {string.Join(", ", Endpoints.Keys)}");
        }
        return ep;
    }

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    /// One JSON-RPC round trip. Protocol errors come back in the payload; transport
    /// failures throw.
    private static async Task<JsonNode?> Rpc(string baseUrl, string version, string method, JsonNode? parameters)
    {
        var body = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = method,
            ["params"] = parameters ?? new JsonObject(),
        };
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var res = await Http.PostAsync($"{baseUrl}/{version}", content).ConfigureAwait(false);
        var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"non-JSON response (HTTP {(int)res.StatusCode}): {ex.Message}");
        }
    }

    private static string RpcErrorText(JsonNode err)
    {
        var msg = err["message"]?.GetValue<string>() ?? "rpc error";
        var code = err["code"]?.ToJsonString();
        var data = err["data"]?.ToJsonString() ?? string.Empty;
        return code is null ? $"{msg} {data}" : $"{msg} {data} ({code})";
    }

    private static string FlagKey(string name) => name.TrimStart('-').Replace("-", "_");

    private static (VerbSpec Spec, List<string> Remaining) ParseVerb(List<string> tokens)
    {
        if (tokens.Count == 0)
        {
            throw new UsageException(
                "no verb given — run `accumulate --help --json` for the command tree");
        }
        var head = tokens[0];
        if (Groups.Contains(head))
        {
            if (tokens.Count < 2)
            {
                throw new UsageException($"'{head}' is a command group; it needs a subcommand");
            }
            var name = $"{head} {tokens[1]}";
            var found = Verbs.FirstOrDefault(v => v.Name == name)
                ?? throw new UsageException($"unknown subcommand '{tokens[1]}' for group '{head}'");
            return (found, tokens.Skip(2).ToList());
        }
        var verb = Verbs.FirstOrDefault(v => v.Name == head)
            ?? throw new UsageException(
                $"unknown verb '{head}' — run `accumulate --help --json` for the command tree");
        return (verb, tokens.Skip(1).ToList());
    }

    private static Dictionary<string, object> ParseVerbArgs(VerbSpec spec, List<string> tokens)
    {
        var outv = new Dictionary<string, object>();
        foreach (var f in spec.Flags)
        {
            var key = FlagKey(f.Name);
            if (f.Default is not null) outv[key] = f.Default;
            if (f.Repeatable) outv[key] = new List<string>();
        }

        var positional = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.StartsWith("--", StringComparison.Ordinal))
            {
                var f = spec.Flags.FirstOrDefault(x => x.Name == t)
                    ?? throw new UsageException($"unknown flag '{t}' for '{spec.Name}'");
                if (i + 1 >= tokens.Count) throw new UsageException($"flag '{t}' expects a value");
                var raw = tokens[++i];
                var key = FlagKey(t);
                if (f.Repeatable)
                {
                    ((List<string>)outv[key]).Add(raw);
                }
                else
                {
                    if (f.Type == "integer" && !long.TryParse(raw, out _))
                        throw new UsageException($"flag '{t}' expects an integer, got '{raw}'");
                    if (f.Type == "number" && !double.TryParse(raw, out _))
                        throw new UsageException($"flag '{t}' expects a number, got '{raw}'");
                    outv[key] = raw;
                }
            }
            else
            {
                positional.Add(t);
            }
        }

        for (var i = 0; i < spec.Args.Length && i < positional.Count; i++)
        {
            outv[spec.Args[i].Name] = positional[i];
        }
        if (positional.Count > spec.Args.Length)
        {
            throw new UsageException($"unexpected arguments for '{spec.Name}': " +
                string.Join(" ", positional.Skip(spec.Args.Length)));
        }
        foreach (var a in spec.Args)
        {
            if (a.Required && !outv.ContainsKey(a.Name))
                throw new UsageException($"'{spec.Name}' requires <{a.Name}>");
        }
        foreach (var f in spec.Flags)
        {
            if (f.Required && !outv.ContainsKey(FlagKey(f.Name)))
                throw new UsageException($"'{spec.Name}' requires {f.Name}");
        }
        return outv;
    }

    private static string? Str(Dictionary<string, object> a, string k) =>
        a.TryGetValue(k, out var v) ? v as string : null;

    private static long Int(Dictionary<string, object> a, string k) =>
        long.TryParse(Str(a, k), out var v) ? v : 0;

    private static JsonNode CommandTree()
    {
        var verbs = new JsonArray();
        foreach (var v in Verbs)
        {
            var args = new JsonArray();
            foreach (var x in v.Args)
                args.Add(new JsonObject { ["name"] = x.Name, ["type"] = x.Type, ["required"] = x.Required });
            var flags = new JsonArray();
            foreach (var f in v.Flags)
            {
                var o = new JsonObject
                {
                    ["name"] = f.Name, ["type"] = f.Type, ["required"] = f.Required,
                };
                if (f.Default is not null) o["default"] = f.Default;
                if (f.Repeatable) o["repeatable"] = true;
                flags.Add(o);
            }
            verbs.Add(new JsonObject
            {
                ["name"] = v.Name, ["summary"] = v.Summary,
                ["network"] = v.Network, ["signs"] = v.Signs,
                ["args"] = args, ["flags"] = flags,
            });
        }
        return new JsonObject
        {
            ["command"] = "accumulate",
            ["envelopeVersion"] = EnvelopeVersion,
            ["globalFlags"] = new JsonArray
            {
                new JsonObject { ["name"] = "--json", ["type"] = "boolean", ["summary"] = "Emit one envelope object on stdout" },
                new JsonObject { ["name"] = "--network", ["type"] = "string", ["default"] = DefaultNetwork, ["summary"] = "Target network; mainnet also requires ACCUMULATE_ALLOW_MAINNET=1" },
                new JsonObject { ["name"] = "--help", ["type"] = "boolean", ["summary"] = "Show help; with --json returns the command tree" },
            },
            ["verbs"] = verbs,
        };
    }

    private static async Task<int> RunVerb(VerbSpec spec, Dictionary<string, object> a, string network)
    {
        switch (spec.Name)
        {
            case "version":
                return Ok(new JsonObject
                {
                    ["sdk"] = SdkName, ["version"] = SdkVersion, ["envelope"] = EnvelopeVersion,
                });

            case "net list":
            {
                var nets = new JsonArray();
                foreach (var (id, ep) in Endpoints)
                {
                    var o = new JsonObject
                    {
                        ["id"] = id, ["endpoint"] = ep,
                        ["faucet"] = id != "mainnet", ["default"] = id == DefaultNetwork,
                    };
                    if (id == "mainnet") o["requiresOptIn"] = true;
                    nets.Add(o);
                }
                return Ok(new JsonObject { ["networks"] = nets });
            }

            case "keys generate":
            {
                var algorithm = (Str(a, "algorithm") ?? "ed25519").ToLowerInvariant();
                if (algorithm != "ed25519")
                {
                    throw new UsageException(
                        $"unsupported algorithm '{algorithm}' — only ed25519 is supported");
                }
                // Uses the SDK's own derivation so the lite address carries its
                // checksum; an address missing it looks right and is rejected on chain.
                // SignatureType lives in the .Generated namespace (Java-mirroring layout).
                // SignatureKeyPair writes diagnostics ("[SignatureKeyPair] Derived
                // pubkey...") straight to stdout, which would corrupt the envelope.
                // stdout is protocol here, so capture anything the SDK prints during
                // the call and forward it to stderr where diagnostics belong.
                // Console.Out is already quarantined for the whole run in Main.
                var principal = Acme.Net.Sdk.Protocol.LiteTokenAccountPrincipal.Generate(
                    Acme.Net.Sdk.Protocol.Generated.SignatureType.ED25519);
                var pub = principal.SignatureKeyPair.GetPublicKey();
                var lid = Acme.Net.Sdk.Protocol.UrlUtils.ComputeLiteIdentityUrl(pub).ToString();
                return Ok(new JsonObject
                {
                    ["algorithm"] = "ed25519",
                    ["publicKey"] = Convert.ToHexString(pub).ToLowerInvariant(),
                    ["liteIdentity"] = lid,
                    ["liteTokenAccount"] = $"{lid}/ACME",
                });
            }

            case "tx build":
            {
                var ps = new JsonObject();
                var flat = new Dictionary<string, object>();
                if (a.TryGetValue("param", out var raws) && raws is List<string> list)
                {
                    foreach (var raw in list)
                    {
                        var idx = raw.IndexOf('=', StringComparison.Ordinal);
                        if (idx < 0)
                            throw new UsageException($"--param must be key=value, got '{raw}'");
                        ps[raw[..idx]] = raw[(idx + 1)..];
                        flat[raw[..idx]] = raw[(idx + 1)..];
                    }
                }
                var op = Str(a, "op") ?? string.Empty;
                var built = BuildBody(op, flat);
                var bodyNode = JsonSerializer.SerializeToNode(built);
                var outPath = Str(a, "out");
                if (outPath is not null) File.WriteAllText(outPath, bodyNode?.ToJsonString() ?? "{}");
                return Ok(new JsonObject
                {
                    ["op"] = op, ["params"] = ps, ["body"] = bodyNode, ["signed"] = false,
                    ["out"] = outPath,
                    ["note"] = "unsigned body; sign it with `tx sign --body <file>`, then `tx submit`",
                });
            }
        }

        var baseUrl = BaseUrl(network);

        // V3 takes {"scope": <url>}. Verified against a live node.
        async Task<JsonNode?> Query(string scope) =>
            await Rpc(baseUrl, "v3", "query", new JsonObject { ["scope"] = scope }).ConfigureAwait(false);

        switch (spec.Name)
        {
            case "query":
            {
                var url = Str(a, "url")!;
                var r = await Query(url).ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                return Ok(new JsonObject { ["url"] = url, ["account"] = r?["result"]?.DeepClone() });
            }

            case "balance":
            {
                var url = Str(a, "url")!;
                var r = await Query(url).ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                return Ok(new JsonObject
                {
                    ["url"] = url,
                    ["balance"] = r?["result"]?["account"]?["balance"]?.DeepClone(),
                    ["raw"] = r?["result"]?.DeepClone(),
                });
            }

            case "chain":
            {
                var url = Str(a, "url")!;
                var parameters = new JsonObject
                {
                    ["scope"] = url,
                    ["query"] = new JsonObject
                    {
                        ["queryType"] = "chain",
                        ["name"] = Str(a, "chain") ?? "main",
                        ["range"] = new JsonObject
                        {
                            ["start"] = Int(a, "start"), ["count"] = Int(a, "count"),
                        },
                    },
                };
                var r = await Rpc(baseUrl, "v3", "query", parameters).ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                return Ok(new JsonObject
                {
                    ["url"] = url, ["chain"] = Str(a, "chain"),
                    ["start"] = Int(a, "start"), ["count"] = Int(a, "count"),
                    ["entries"] = r?["result"]?.DeepClone(),
                });
            }

            case "faucet":
            {
                var url = Str(a, "url")!;
                var r = await Rpc(baseUrl, "v2", "faucet",
                    new JsonObject { ["url"] = url }).ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                return Ok(new JsonObject { ["url"] = url, ["result"] = r?["result"]?.DeepClone() });
            }

            case "credits estimate":
            {
                var r = await Query("acc://dn.acme/oracle").ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                return Ok(new JsonObject
                {
                    ["url"] = Str(a, "url"), ["acme"] = Str(a, "amount"),
                    ["oracle"] = r?["result"]?.DeepClone(),
                    ["note"] = "credits = acme * oraclePrice / 1e8 (oracle is unscaled)",
                });
            }

            case "tx status":
            {
                var txid = Str(a, "txid")!;
                var r = await Query(txid).ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                return Ok(new JsonObject { ["txid"] = txid, ["status"] = r?["result"]?.DeepClone() });
            }

            case "tx wait":
            {
                var txid = Str(a, "txid")!;
                var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, Int(a, "timeout")));
                while (DateTime.UtcNow < deadline)
                {
                    var r = await Query(txid).ConfigureAwait(false);
                    if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                    var status = r?["result"]?["status"]?["code"]?.GetValue<string>();
                    if (status is "delivered" or "failed")
                    {
                        return Ok(new JsonObject
                        {
                            ["txid"] = txid, ["final"] = true, ["status"] = status,
                            ["raw"] = r?["result"]?.DeepClone(),
                        });
                    }
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                return Fail($"timed out waiting for {txid} to reach a final state",
                    "ACC_NETWORK_UNAVAILABLE", ExitFailed);
            }

            case "net status":
            {
                // A protocol rejection still proves the node answered, so only a
                // transport failure counts as unreachable — that is what exit 3 means.
                var r = await Query("acc://dn.acme").ConfigureAwait(false);
                var o = new JsonObject
                {
                    ["network"] = network, ["endpoint"] = baseUrl, ["reachable"] = true,
                };
                if (r?["error"] is { } err) o["probeError"] = RpcErrorText(err);
                else o["probe"] = r?["result"]?.DeepClone();
                return Ok(o);
            }

            case "tx sign":
            {
                // The ONLY verb that signs. Delegates to the SDK signer: signing
                // bytes are consensus-visible and a second implementation is how
                // they drift.
                var privateHex = LoadPrivateKey(a);
                var bodyPath = Str(a, "body");
                var envPath = Str(a, "envelope");
                if ((bodyPath is null) == (envPath is null))
                {
                    throw new UsageException(
                        "pass exactly one of --body (start a new transaction) or " +
                        "--envelope (co-sign an existing one for an M-of-N threshold)");
                }

                byte[] seed;
                try { seed = Convert.FromHexString(privateHex); }
                catch (FormatException) { throw new UsageException("private key is not valid hex"); }
                if (!Acme.Net.Sdk.Signing.SignatureKeyPair.TryImportFromSecretKeyBytes(
                        seed, Acme.Net.Sdk.Protocol.Generated.SignatureType.ED25519, out var kp))
                {
                    throw new UsageException("private key must be a 32- or 64-byte ed25519 hex seed");
                }

                using var v3 = new Acme.Net.Sdk.V3.AccumulateV3Client($"{baseUrl}/v3");
                var signer = new Acme.Net.Sdk.Signing.SmartSigner(v3, kp, Str(a, "signer")!);

                Dictionary<string, object?> envelope;
                bool cosigned;
                if (envPath is not null)
                {
                    if (!File.Exists(envPath)) throw new UsageException($"no such envelope file: {envPath}");
                    var envJson = await File.ReadAllTextAsync(envPath).ConfigureAwait(false);
                    var existing = ReadEnvelope(envJson);
                    envelope = await signer.SignExistingAsync(existing).ConfigureAwait(false);
                    cosigned = true;
                }
                else
                {
                    if (!File.Exists(bodyPath)) throw new UsageException($"no such body file: {bodyPath}");
                    if (Str(a, "principal") is null)
                        throw new UsageException("--principal is required when signing a --body");
                    var bodyJson = await File.ReadAllTextAsync(bodyPath!).ConfigureAwait(false);
                    var body = JsonSerializer.Deserialize<Dictionary<string, object?>>(bodyJson)
                        ?? throw new UsageException("body is not a JSON object");
                    envelope = await signer
                        .SignAsync(Str(a, "principal")!, body)
                        .ConfigureAwait(false);
                    cosigned = false;
                }

                var envNode = JsonSerializer.SerializeToNode(envelope);
                var sigCount = (envelope.TryGetValue("signatures", out var sc) && sc is List<object?> l)
                    ? l.Count : 0;
                var signOut = Str(a, "out");
                if (signOut is not null) File.WriteAllText(signOut, envNode?.ToJsonString() ?? "{}");
                return Ok(new JsonObject
                {
                    ["signed"] = true, ["cosigned"] = cosigned, ["signatures"] = sigCount,
                    ["principal"] = Str(a, "principal"),
                    ["signer"] = Str(a, "signer"), ["envelope"] = envNode, ["out"] = signOut,
                });
            }

            case "tx submit":
            {
                // Deliberately does NOT sign, and no longer pretends to: it used
                // to take --key-file/--key-env and never use them.
                var path = Str(a, "envelope")!;
                if (!File.Exists(path))
                    throw new UsageException($"no such envelope file: {path}");
                var envelope = JsonNode.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
                // V3 submit takes {"envelope": <envelope>}; posting the bare
                // envelope returns -33400 "envelope is missing".
                var r = await Rpc(baseUrl, "v3", "submit",
                    new JsonObject { ["envelope"] = envelope }).ConfigureAwait(false);
                if (r?["error"] is { } err) return Fail(RpcErrorText(err));
                // A response without an RPC error does NOT mean the transaction was
                // accepted: V3 returns one status per message, and a rejected
                // envelope shows up as `failed: true` inside them. Reporting that as
                // success is the "submitted != delivered" trap.
                var failures = new List<string>();
                if (r?["result"] is JsonArray items)
                {
                    foreach (var item in items)
                    {
                        var st = item?["status"];
                        if (st?["failed"]?.GetValue<bool>() == true)
                        {
                            failures.Add(st?["error"]?["message"]?.GetValue<string>()
                                ?? st?["code"]?.GetValue<string>() ?? "unknown");
                        }
                    }
                }
                if (failures.Count > 0) return Fail(string.Join("; ", failures));
                return Ok(new JsonObject
                {
                    ["submitted"] = true, ["result"] = r?["result"]?.DeepClone(),
                });
            }

            default:
                throw new UsageException($"unknown verb '{spec.Name}'");
        }
    }

    public static async Task<int> Main(string[] argv)
    {
        // Anything the SDK writes to Console.Out lands in this buffer instead of
        // the protocol stream, and is forwarded to stderr on the way out.
        RealOut = Console.Out;
        var sdkChatter = new StringWriter();
        Console.SetOut(sdkChatter);
        try
        {
            return await RunAsync(argv).ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(RealOut);
            var captured = sdkChatter.ToString();
            if (captured.Length > 0) Console.Error.Write(captured);
        }
    }

    private static async Task<int> RunAsync(string[] argv)
    {
        _asJson = argv.Contains("--json");
        var network = DefaultNetwork;
        var ni = Array.IndexOf(argv, "--network");
        if (ni > -1)
        {
            if (ni + 1 >= argv.Length)
            {
                _network = null;
                return Fail("flag '--network' expects a value", "ACC_USAGE");
            }
            network = argv[ni + 1];
        }
        _network = network;

        var tokens = new List<string>();
        for (var i = 0; i < argv.Length; i++)
        {
            if (argv[i] == "--json") continue;
            if (i == ni) { i++; continue; }
            tokens.Add(argv[i]);
        }

        var wantsHelp = tokens.Contains("--help") || tokens.Contains("-h");
        var verbTokens = tokens
            .Where(t => t != "--help" && t != "-h" && t != "--version").ToList();

        if (wantsHelp || verbTokens.Count == 0)
        {
            if (_asJson) return Ok(CommandTree());
            RealOut.WriteLine("accumulate — Accumulate SDK CLI\n");
            foreach (var v in Verbs) RealOut.WriteLine($"  {v.Name,-20} {v.Summary}");
            RealOut.WriteLine("\nRun with --json --help for the machine-readable command tree.");
            return ExitOk;
        }

        try
        {
            var (spec, rest) = ParseVerb(verbTokens);
            var a = ParseVerbArgs(spec, rest);
            return await RunVerb(spec, a, network).ConfigureAwait(false);
        }
        catch (UsageException e)
        {
            return Fail(e.Message, "ACC_USAGE");
        }
        catch (Exception e)
        {
            var raw = $"{e.GetType().Name}: {e.Message}";
            var code = Classify(raw);
            return Fail(raw, code, code == "ACC_NETWORK_UNAVAILABLE" ? ExitNetwork : ExitFailed);
        }
    }
}
