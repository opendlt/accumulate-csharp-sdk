using System.Text.Json;
using Acme.Net.Sdk.Codec;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.V3;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// One participant in a multi-signature transaction: a signer plus the vote/reason/evidence
    /// they attach to THEIR OWN signature (signature memo = tag 9, signature data = tag 10).
    /// </summary>
    public sealed record MultiSigParticipant(
        SmartSigner Signer,
        VoteType Vote = VoteType.Accept,
        string? SignatureMemo = null,
        byte[]? SignatureData = null);

    /// <summary>
    /// The result of initiating (but not completing) an M-of-N transaction: the envelope to submit
    /// to make the transaction PENDING on the network, plus everything an independent co-signer
    /// needs to add their signature later (the transaction hash and the principal it is pending on).
    /// Share <see cref="TransactionHashHex"/> + <see cref="Principal"/> out-of-band with each
    /// authority; they sign with <see cref="SmartSigner.SignRemoteSubmitAndWaitAsync(byte[], string, Acme.Net.Sdk.Protocol.VoteType?, string?, byte[]?, int, System.TimeSpan?)"/>.
    /// </summary>
    public sealed record InitiatedTransaction(
        Dictionary<string, object?> Envelope,
        byte[] TransactionHash,
        string TransactionHashHex,
        string Principal,
        Dictionary<string, object?> Header,
        Dictionary<string, object?> Body);

    /// <summary>
    /// Builds and submits a single transaction co-signed by N participants (M-of-N key page).
    /// Every participant signs the SAME transaction hash; the initiator's metadata hash sets the
    /// header initiator. Each participant's vote/memo/data is bound into their own signature.
    /// <para>
    /// Two flows are supported:
    /// <list type="bullet">
    /// <item><b>Synchronous co-sign</b> (<see cref="BuildEnvelopeAsync"/> / <see cref="SubmitAsync"/>):
    /// all participants' keys are available in one process at one time (custodial).</item>
    /// <item><b>Asynchronous / independent</b> (<see cref="InitiateAsync"/> + each authority's
    /// <c>SmartSigner.SignRemote*</c>): the initiator makes the transaction pending and shares its
    /// hash; each authority signs later from their own wallet/process.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class MultiSig
    {
        /// <summary>
        /// Build the INITIATOR's envelope for an M-of-N transaction (a single signature). Submitting
        /// the returned <see cref="InitiatedTransaction.Envelope"/> leaves the transaction PENDING
        /// until the key page threshold is met. The returned hash/principal are what each independent
        /// co-signer needs to add their signature asynchronously (see <c>SmartSigner.SignRemoteAsync</c>).
        /// </summary>
        public static async Task<InitiatedTransaction> InitiateAsync(
            string principal,
            Dictionary<string, object?> body,
            MultiSigParticipant initiator,
            string? headerMemo = null,
            byte[]? headerMetadata = null)
        {
            var initMeta = await initiator.Signer
                .ComputeMetadataAsync(initiator.SignatureMemo, initiator.SignatureData, initiator.Vote)
                .ConfigureAwait(false);

            var header = SmartSigner.BuildHeader(principal, initMeta.MetadataHash, headerMemo, headerMetadata);
            var txHash = TransactionCodec.ComputeTransactionHash(header, body);
            var signature = initiator.Signer.BuildSignature(txHash, initMeta);

            var envelope = new Dictionary<string, object?>
            {
                ["signatures"] = new List<object?> { signature },
                ["transaction"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["header"] = header, ["body"] = body }
                },
            };

            return new InitiatedTransaction(
                envelope, txHash, Convert.ToHexString(txHash).ToLowerInvariant(), principal, header, body);
        }

        /// <summary>
        /// Assemble the co-signed envelope (without submitting). Useful for tests / offline review.
        /// </summary>
        public static async Task<Dictionary<string, object?>> BuildEnvelopeAsync(
            string principal,
            Dictionary<string, object?> body,
            MultiSigParticipant initiator,
            IEnumerable<MultiSigParticipant> coSigners,
            string? headerMemo = null,
            byte[]? headerMetadata = null)
        {
            // 1. Initiator metadata first — its hash defines the header initiator (and thus the txHash).
            var initMeta = await initiator.Signer
                .ComputeMetadataAsync(initiator.SignatureMemo, initiator.SignatureData, initiator.Vote)
                .ConfigureAwait(false);

            var header = SmartSigner.BuildHeader(principal, initMeta.MetadataHash, headerMemo, headerMetadata);
            var txHash = TransactionCodec.ComputeTransactionHash(header, body);

            // 2. Every participant signs the SAME txHash with their own metadata.
            var signatures = new List<object?> { initiator.Signer.BuildSignature(txHash, initMeta) };
            foreach (var p in coSigners)
            {
                var meta = await p.Signer.ComputeMetadataAsync(p.SignatureMemo, p.SignatureData, p.Vote).ConfigureAwait(false);
                signatures.Add(p.Signer.BuildSignature(txHash, meta));
            }

            return new Dictionary<string, object?>
            {
                ["signatures"] = signatures,
                ["transaction"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["header"] = header, ["body"] = body }
                },
            };
        }

        /// <summary>Build + submit the co-signed transaction. Returns the first submission result.</summary>
        public static async Task<JsonElement> SubmitAsync(
            AccumulateV3Client client,
            string principal,
            Dictionary<string, object?> body,
            MultiSigParticipant initiator,
            IEnumerable<MultiSigParticipant> coSigners,
            string? headerMemo = null,
            byte[]? headerMetadata = null)
        {
            var envelope = await BuildEnvelopeAsync(principal, body, initiator, coSigners, headerMemo, headerMetadata)
                .ConfigureAwait(false);
            var results = await client.SubmitAsync(envelope).ConfigureAwait(false);
            return results.Count > 0 ? results[0] : default;
        }
    }
}
