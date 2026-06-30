using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Net.Sdk.Exceptions;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.V3;

namespace Acme.Net.Sdk.Provisioning
{
    /// <summary>
    /// Idempotent, staged provisioner for arbitrary-depth Accumulate identity hierarchies.
    ///
    /// <para>
    /// Accumulate accounts are NOT a flat namespace under an ADI, but they are also not freely
    /// nestable: every create transaction must be signed with a principal equal to the
    /// <em>immediate parent</em> of the URL being created, and that parent must already exist as an
    /// ADI (Go core <c>internal/core/execute/v2/chain/create_utils.go:originIsParent</c> +
    /// <c>checkCreateAdiAccount</c>). So a path like <c>acc://verso-acme.acme/ospri/inventory</c> is
    /// valid, but only as a two-step provision: first create <c>.../ospri</c> as a sub-ADI (signed
    /// by the root ADI), then create <c>.../ospri/inventory</c> (signed by <c>.../ospri</c>).
    /// </para>
    ///
    /// <para>
    /// <see cref="EnsurePathAsync"/> generalises that to any depth: it walks the path root→leaf,
    /// creates each missing intermediate as a sub-ADI (switching the signing principal to that
    /// level's parent at every step), then creates the leaf. It is idempotent — each node is
    /// queried first and skipped if it already exists — so partial runs resume cleanly.
    /// </para>
    ///
    /// <para>
    /// Custody is decided per level (<see cref="CustodyPlan"/>). With the default
    /// <see cref="CustodyMode.InheritParent"/>, one signer (the root key page) signs and pays for
    /// the entire chain; the executor resolves authority by walking up the identity chain. With
    /// <see cref="CustodyMode.OwnKeyBook"/>, a level gets its own key book + funded key page and
    /// becomes the signer for everything beneath it.
    /// </para>
    /// </summary>
    public sealed class HierarchyProvisioner
    {
        private readonly AccumulateV3Client _client;

        public HierarchyProvisioner(AccumulateV3Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Ensure every account along <paramref name="targetUrl"/> exists, creating whatever is
        /// missing. The intermediate segments become sub-ADIs; the final segment becomes
        /// <paramref name="leafKind"/>.
        /// </summary>
        /// <param name="targetUrl">e.g. <c>acc://verso-acme.acme/ospri/inventory</c>. Must have at least one segment below the ADI.</param>
        /// <param name="rootSigner">A signer for the root ADI's key page (its credits pay for every <c>InheritParent</c> level).</param>
        /// <param name="leafKind">What the last segment is created as. Defaults to a data account.</param>
        /// <param name="custody">Per-level custody for intermediates. Defaults to inherit-parent for all levels.</param>
        /// <param name="leafOptions">Extra inputs for the leaf (token URL, authorities, key pair, sub-ADI custody).</param>
        /// <param name="creditFunder">Required only if any level uses <see cref="CustodyMode.OwnKeyBook"/> — funds each new key page.</param>
        /// <param name="memoPrefix">Optional memo prefix attached to every create transaction.</param>
        public async Task<ProvisionResult> EnsurePathAsync(
            string targetUrl,
            SmartSigner rootSigner,
            LeafKind leafKind = LeafKind.DataAccount,
            CustodyPlan? custody = null,
            LeafOptions? leafOptions = null,
            CreditFunderAsync? creditFunder = null,
            string? memoPrefix = null)
        {
            if (rootSigner == null) throw new ArgumentNullException(nameof(rootSigner));
            var (root, segments) = ParsePath(targetUrl);
            if (segments.Length == 0)
                throw new AccumulateValidationException(
                    $"target '{targetUrl}' has no child path below the ADI — nothing to provision", nameof(targetUrl));

            custody ??= new CustodyPlan();

            // Sanity: the root ADI must already exist and be an identity, and the signer must live under it.
            var rootType = await GetAccountTypeAsync(root).ConfigureAwait(false);
            if (rootType == null)
                throw new AccumulateException(
                    $"root ADI {root} does not exist — create it (and fund its key page) before provisioning children");
            if (!IsIdentityType(rootType))
                throw new AccumulateException($"root {root} is not an ADI (type '{rootType}')");
            if (!StartsWithSegment(rootSigner.SignerUrl, root))
                throw new AccumulateValidationException(
                    $"rootSigner ({rootSigner.SignerUrl}) is not under the target's root ADI ({root})", nameof(rootSigner));

            var nodes = new List<ProvisionedNode>(segments.Length);
            SmartSigner controller = rootSigner;   // controls `parentUrl`
            string parentUrl = root;

            for (int i = 0; i < segments.Length; i++)
            {
                string childUrl = parentUrl + "/" + segments[i];
                bool isLeaf = i == segments.Length - 1;

                if (!isLeaf)
                {
                    var levelCustody = custody.For(childUrl);
                    nodes.Add(await EnsureSubAdiAsync(childUrl, parentUrl, controller, levelCustody, creditFunder, memoPrefix)
                        .ConfigureAwait(false));

                    // Hand control to this level's own key page if it has independent custody;
                    // otherwise the inherited controller keeps signing for everything below.
                    if (levelCustody.Mode == CustodyMode.OwnKeyBook)
                        controller = new SmartSigner(_client, levelCustody.KeyBookKeyPair!, KeyPageUrl(childUrl, levelCustody.KeyBookName));
                }
                else
                {
                    nodes.Add(await EnsureLeafAsync(childUrl, parentUrl, controller, leafKind, leafOptions, creditFunder, memoPrefix)
                        .ConfigureAwait(false));
                }

                parentUrl = childUrl;
            }

            return new ProvisionResult { Target = targetUrl, RootAdi = root, Nodes = nodes };
        }

        // ------------------------------------------------------------------
        // Sub-ADI
        // ------------------------------------------------------------------

        private async Task<ProvisionedNode> EnsureSubAdiAsync(
            string url, string parentUrl, SmartSigner parentController,
            LevelCustody custody, CreditFunderAsync? funder, string? memoPrefix)
        {
            string? pageUrl = custody.Mode == CustodyMode.OwnKeyBook
                ? KeyPageUrl(url, custody.KeyBookName)
                : null;

            var existing = await GetAccountTypeAsync(url).ConfigureAwait(false);
            bool created = false;

            if (existing != null)
            {
                if (!IsIdentityType(existing))
                    throw new AccumulateException(
                        $"cannot use {url} as an intermediate ADI: it already exists as type '{existing}'. " +
                        "Accounts are terminal in Accumulate and cannot have children — choose a different path or custody.");
            }
            else
            {
                Dictionary<string, object?> body;
                if (custody.Mode == CustodyMode.OwnKeyBook)
                {
                    if (custody.KeyBookKeyPair == null)
                        throw new AccumulateValidationException(
                            $"OwnKeyBook custody for {url} requires a KeyBookKeyPair", nameof(custody));
                    string keyBookUrl = $"{url}/{custody.KeyBookName}";
                    string keyHash = Sha256Hex(custody.KeyBookKeyPair.GetPublicKey());
                    body = TxBody.CreateIdentity(url, keyBookUrl, keyHash);
                }
                else
                {
                    body = TxBody.CreateIdentityInherited(url);
                }

                var res = await parentController.SignSubmitAndWaitAsync(
                    parentUrl, body, memo: Memo(memoPrefix, $"create sub-ADI {url}")).ConfigureAwait(false);
                Require(res, $"create sub-ADI {url}");
                created = true;
            }

            // For an independent-custody level, make sure its key page can pay before anyone signs
            // with it. Only fund when the page is empty — that covers both a fresh create and a
            // resume where the identity exists but funding never landed — so re-runs don't overspend.
            if (custody.Mode == CustodyMode.OwnKeyBook)
            {
                long balance = await GetCreditBalanceAsync(pageUrl!).ConfigureAwait(false);
                if (balance <= 0)
                {
                    if (custody.Credits <= 0)
                        throw new AccumulateValidationException(
                            $"OwnKeyBook custody for {url} requires Credits > 0 to fund {pageUrl}", nameof(custody));
                    if (funder == null)
                        throw new AccumulateValidationException(
                            $"OwnKeyBook custody for {url} requires a creditFunder to fund {pageUrl}", nameof(funder));
                    await funder(pageUrl!, custody.Credits).ConfigureAwait(false);
                    await WaitForCreditsAsync(pageUrl!, 1).ConfigureAwait(false);
                }
            }

            return new ProvisionedNode(url, NodeKind.SubAdi, created, custody.Mode);
        }

        // ------------------------------------------------------------------
        // Leaf
        // ------------------------------------------------------------------

        private async Task<ProvisionedNode> EnsureLeafAsync(
            string url, string parentUrl, SmartSigner controller,
            LeafKind kind, LeafOptions? opts, CreditFunderAsync? funder, string? memoPrefix)
        {
            // A sub-ADI leaf is just another identity — reuse the sub-ADI path (idempotency, custody, funding).
            if (kind == LeafKind.SubAdi)
                return await EnsureSubAdiAsync(url, parentUrl, controller,
                    opts?.SubAdiCustody ?? LevelCustody.Inherit, funder, memoPrefix).ConfigureAwait(false);

            string expectedType = ExpectedWireType(kind);
            var existing = await GetAccountTypeAsync(url).ConfigureAwait(false);
            if (existing != null)
            {
                if (!string.Equals(existing, expectedType, StringComparison.OrdinalIgnoreCase))
                    throw new AccumulateException(
                        $"{url} already exists as type '{existing}', but a {kind} ('{expectedType}') was requested");
                return new ProvisionedNode(url, ToNodeKind(kind), Created: false, Custody: null);
            }

            Dictionary<string, object?> body = kind switch
            {
                LeafKind.DataAccount => TxBody.CreateDataAccount(url, opts?.Authorities),
                LeafKind.TokenAccount => TxBody.CreateTokenAccount(url, opts?.TokenUrl ?? "acc://ACME", opts?.Authorities),
                LeafKind.KeyBook => TxBody.CreateKeyBook(url,
                    Sha256Hex((opts?.KeyPair ?? throw new AccumulateValidationException(
                        "KeyBook leaf requires LeafOptions.KeyPair", nameof(opts))).GetPublicKey())),
                _ => throw new AccumulateValidationException($"unsupported leaf kind {kind}", nameof(kind)),
            };

            var res = await controller.SignSubmitAndWaitAsync(
                parentUrl, body, memo: Memo(memoPrefix, $"create {expectedType} {url}")).ConfigureAwait(false);
            Require(res, $"create {expectedType} {url}");
            return new ProvisionedNode(url, ToNodeKind(kind), Created: true, Custody: null);
        }

        // ------------------------------------------------------------------
        // Queries / helpers
        // ------------------------------------------------------------------

        /// <summary>Returns the account's wire type string, or <c>null</c> if it does not exist.</summary>
        private async Task<string?> GetAccountTypeAsync(string url)
        {
            try
            {
                var r = await _client.QueryAccountAsync(url).ConfigureAwait(false);
                if (r.TryGetProperty("account", out var acc) && acc.ValueKind == JsonValueKind.Object &&
                    acc.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString();
                if (r.TryGetProperty("type", out var t2) && t2.ValueKind == JsonValueKind.String)
                    return t2.GetString();
                return "unknown"; // exists, but the record shape was unexpected
            }
            catch (AccumulateApiException ex) when (IsNotFound(ex))
            {
                return null;
            }
        }

        private async Task<long> GetCreditBalanceAsync(string url)
        {
            try
            {
                var r = await _client.QueryAccountAsync(url).ConfigureAwait(false);
                if (r.TryGetProperty("account", out var acc) &&
                    acc.TryGetProperty("creditBalance", out var cb))
                {
                    var s = cb.GetRawText().Trim('"');
                    if (long.TryParse(s, out var v)) return v;
                }
            }
            catch (AccumulateApiException ex) when (IsNotFound(ex)) { }
            return 0;
        }

        private async Task WaitForCreditsAsync(string url, long min, int maxAttempts = 30, TimeSpan? interval = null)
        {
            var iv = interval ?? TimeSpan.FromSeconds(2);
            for (int i = 0; i < maxAttempts; i++)
            {
                if (await GetCreditBalanceAsync(url).ConfigureAwait(false) >= min)
                    return;
                await Task.Delay(iv).ConfigureAwait(false);
            }
            throw new AccumulateTimeoutException($"key page {url} did not reach {min} credit(s) within the polling window");
        }

        // ------------------------------------------------------------------
        // Pure helpers
        // ------------------------------------------------------------------

        /// <summary>Split <c>acc://authority/seg1/seg2/...</c> into the root ADI URL and the child segments.</summary>
        internal static (string Root, string[] Segments) ParsePath(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new AccumulateValidationException("target URL is required", nameof(target));

            const string scheme = "acc://";
            var s = target.Trim();
            if (!s.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                throw new AccumulateValidationException($"target must start with 'acc://' — got '{target}'", nameof(target));

            var rest = s.Substring(scheme.Length).TrimEnd('/');
            int slash = rest.IndexOf('/');
            if (slash < 0)
                return (scheme + rest, Array.Empty<string>());

            var authority = rest.Substring(0, slash);
            var path = rest.Substring(slash + 1);
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return (scheme + authority, segments);
        }

        private static string KeyPageUrl(string adiUrl, string keyBookName) => $"{adiUrl}/{keyBookName}/1";

        private static string Sha256Hex(byte[] data)
            => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

        private static string? Memo(string? prefix, string what) => prefix == null ? what : $"{prefix}: {what}";

        private static bool IsIdentityType(string? type)
            => string.Equals(type, "identity", StringComparison.OrdinalIgnoreCase);

        private static bool IsNotFound(AccumulateApiException ex)
        {
            var m = ex.Message ?? string.Empty;
            return m.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                || m.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool StartsWithSegment(string url, string root)
            => url.Equals(root, StringComparison.OrdinalIgnoreCase)
            || url.StartsWith(root.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);

        private static string ExpectedWireType(LeafKind kind) => kind switch
        {
            LeafKind.DataAccount => "dataAccount",
            LeafKind.TokenAccount => "tokenAccount",
            LeafKind.KeyBook => "keyBook",
            LeafKind.SubAdi => "identity",
            _ => throw new AccumulateValidationException($"unsupported leaf kind {kind}", nameof(kind)),
        };

        private static NodeKind ToNodeKind(LeafKind kind) => kind switch
        {
            LeafKind.DataAccount => NodeKind.DataAccount,
            LeafKind.TokenAccount => NodeKind.TokenAccount,
            LeafKind.KeyBook => NodeKind.KeyBook,
            LeafKind.SubAdi => NodeKind.SubAdi,
            _ => throw new AccumulateValidationException($"unsupported leaf kind {kind}", nameof(kind)),
        };

        private static void Require(TransactionResult res, string what)
        {
            if (res == null || !res.Success)
                throw new AccumulateException($"{what} failed: {res?.Error ?? "unknown error"}");
        }
    }
}
