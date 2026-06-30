using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Net.Sdk.Signing;

namespace Acme.Net.Sdk.Provisioning
{
    /// <summary>
    /// What the FINAL segment of a path should be created as. Every segment ABOVE the leaf is
    /// always a sub-ADI (accounts are terminal in Accumulate and cannot be nested under).
    /// </summary>
    public enum LeafKind
    {
        DataAccount,
        TokenAccount,
        KeyBook,
        SubAdi,
    }

    /// <summary>
    /// How an intermediate sub-ADI is governed.
    /// <list type="bullet">
    /// <item><see cref="InheritParent"/>: the sub-ADI is created with an empty authority set and
    /// inherits the parent identity's key book (resolved by the executor walking up the identity
    /// chain). The same (parent / root) key page signs and pays for everything below it — no extra
    /// keys, no extra credits. This is the default and the common case.</item>
    /// <item><see cref="OwnKeyBook"/>: the sub-ADI is created with its OWN key book + key page
    /// seeded with a caller-supplied key. That gives the level independent signing authority
    /// (e.g. a tenant holding its own keys), but the new key page must be funded with credits
    /// before it can create anything beneath it.</item>
    /// </list>
    /// </summary>
    public enum CustodyMode
    {
        InheritParent,
        OwnKeyBook,
    }

    /// <summary>The kind of node that was provisioned (for reporting).</summary>
    public enum NodeKind
    {
        SubAdi,
        DataAccount,
        TokenAccount,
        KeyBook,
    }

    /// <summary>
    /// Funds <paramref name="keyPageUrl"/> with at least <paramref name="credits"/> credits, paying
    /// in ACME from a caller-controlled source. Used only for <see cref="CustodyMode.OwnKeyBook"/>
    /// levels, whose freshly created key page starts with zero credits. Must not return until the
    /// credits have settled on chain. See <see cref="CreditFunders"/> for a ready-made implementation.
    /// </summary>
    public delegate Task CreditFunderAsync(string keyPageUrl, int credits);

    /// <summary>
    /// Per-level governance for an intermediate sub-ADI created while walking a path.
    /// </summary>
    public sealed class LevelCustody
    {
        public CustodyMode Mode { get; init; } = CustodyMode.InheritParent;

        // ---- OwnKeyBook only ----

        /// <summary>The initial key the new key page is seeded with (its public key hash is stored).</summary>
        public SignatureKeyPair? KeyBookKeyPair { get; init; }

        /// <summary>Name of the key book account created under the sub-ADI. Page 1 is <c>{adi}/{KeyBookName}/1</c>.</summary>
        public string KeyBookName { get; init; } = "book";

        /// <summary>Credits to buy for the new key page so it can create accounts beneath it.</summary>
        public int Credits { get; init; }

        /// <summary>Inherit the parent identity's authority (no new keys / credits). The default.</summary>
        public static LevelCustody Inherit { get; } = new() { Mode = CustodyMode.InheritParent };

        /// <summary>Give this level its own key book + funded key page (independent custody).</summary>
        public static LevelCustody Own(SignatureKeyPair keyBookKeyPair, int credits, string keyBookName = "book")
            => new()
            {
                Mode = CustodyMode.OwnKeyBook,
                KeyBookKeyPair = keyBookKeyPair,
                Credits = credits,
                KeyBookName = keyBookName,
            };
    }

    /// <summary>
    /// Custody decisions for the intermediate sub-ADIs of a path. Defaults to
    /// <see cref="CustodyMode.InheritParent"/> for every level; override specific levels by their
    /// full sub-ADI URL with <see cref="WithLevel"/>.
    /// </summary>
    public sealed class CustodyPlan
    {
        private readonly Dictionary<string, LevelCustody> _perLevel =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Custody used for any level not explicitly overridden.</summary>
        public LevelCustody Default { get; init; } = LevelCustody.Inherit;

        /// <summary>Override the custody for the sub-ADI at <paramref name="subAdiUrl"/> (e.g. <c>acc://verso-acme.acme/ospri</c>).</summary>
        public CustodyPlan WithLevel(string subAdiUrl, LevelCustody custody)
        {
            _perLevel[Normalize(subAdiUrl)] = custody ?? throw new ArgumentNullException(nameof(custody));
            return this;
        }

        /// <summary>Resolve the custody for a given sub-ADI URL.</summary>
        public LevelCustody For(string subAdiUrl)
            => _perLevel.TryGetValue(Normalize(subAdiUrl), out var c) ? c : Default;

        private static string Normalize(string url) => url.Trim().TrimEnd('/');
    }

    /// <summary>Extra inputs for creating the leaf account.</summary>
    public sealed class LeafOptions
    {
        /// <summary>Token URL for a <see cref="LeafKind.TokenAccount"/> leaf.</summary>
        public string TokenUrl { get; init; } = "acc://ACME";

        /// <summary>Explicit authorities for the leaf (optional; default = inherit parent).</summary>
        public List<string>? Authorities { get; init; }

        /// <summary>Seed key for a <see cref="LeafKind.KeyBook"/> leaf, or an <see cref="LeafKind.SubAdi"/> leaf using <see cref="CustodyMode.OwnKeyBook"/>.</summary>
        public SignatureKeyPair? KeyPair { get; init; }

        /// <summary>Custody for a <see cref="LeafKind.SubAdi"/> leaf (default = inherit).</summary>
        public LevelCustody? SubAdiCustody { get; init; }
    }

    /// <summary>One account that the provisioner ensured exists.</summary>
    public sealed record ProvisionedNode(string Url, NodeKind Kind, bool Created, CustodyMode? Custody);

    /// <summary>Result of <c>EnsurePathAsync</c>: the full chain of nodes from the first sub-ADI down to the leaf.</summary>
    public sealed class ProvisionResult
    {
        public string Target { get; init; } = "";
        public string RootAdi { get; init; } = "";
        public IReadOnlyList<ProvisionedNode> Nodes { get; init; } = Array.Empty<ProvisionedNode>();

        /// <summary>The leaf node (last in the chain).</summary>
        public ProvisionedNode Leaf => Nodes[Nodes.Count - 1];

        /// <summary>How many nodes were newly created (vs already existed).</summary>
        public int CreatedCount
        {
            get
            {
                int n = 0;
                foreach (var node in Nodes) if (node.Created) n++;
                return n;
            }
        }
    }
}
