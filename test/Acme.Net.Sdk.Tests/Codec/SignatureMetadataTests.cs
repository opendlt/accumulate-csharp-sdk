using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Acme.Net.Sdk.Codec;
using Acme.Net.Sdk.Protocol;
using Xunit;

namespace Acme.Net.Sdk.Tests.Codec
{
    /// <summary>
    /// Verifies that ED25519 signature-metadata hashing matches Go core's generated marshaler
    /// (types_gen.go): 1=Type, 2=PublicKey, 4=Signer, 5=SignerVersion, 6=Timestamp, 7=Vote,
    /// 9=Memo, 10=Data  (Signature@3 and TransactionHash@8 are cleared in Metadata()).
    ///
    /// These tests re-implement the LEB128 TLV marshaling INDEPENDENTLY with the correct Go tags
    /// and assert the SDK's ComputeSignatureMetadataHash matches — so the historical off-by-one
    /// (Memo/Data emitted at 10/11) would fail here.
    /// </summary>
    public class SignatureMetadataTests
    {
        private const int Ed25519 = 2;
        private static readonly byte[] Pub = Convert.FromHexString(
            "0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20");
        private const string Signer = "acc://verso-acme.acme/book/1";
        private const int Version = 3;
        private const long Timestamp = 1_700_000_000_000_000L;

        // ---- Independent reference marshaler (LEB128, single-byte field tag) ----

        private sealed class Ref
        {
            private readonly List<byte> _b = new();
            public void Tag(int t) => _b.Add((byte)t);
            public void Varint(ulong v) { while (v >= 0x80) { _b.Add((byte)((v & 0x7F) | 0x80)); v >>= 7; } _b.Add((byte)v); }
            public void UInt(int t, ulong v) { Tag(t); Varint(v); }
            public void Bytes(int t, byte[] v) { Tag(t); Varint((ulong)v.Length); _b.AddRange(v); }
            public void Str(int t, string s) => Bytes(t, Encoding.UTF8.GetBytes(s));
            public byte[] Hash() => SHA256.HashData(_b.ToArray());
        }

        private static byte[] ExpectedHash(int vote, string? memo, byte[]? data, bool buggyTags = false)
        {
            var r = new Ref();
            r.UInt(1, (ulong)Ed25519);
            r.Bytes(2, Pub);
            r.Str(4, new Url(Signer).String());   // render signer exactly as the SDK does
            r.UInt(5, (ulong)Version);
            r.UInt(6, (ulong)Timestamp);
            if (vote != 0) r.UInt(7, (ulong)vote);
            int memoTag = buggyTags ? 10 : 9;
            int dataTag = buggyTags ? 11 : 10;
            if (!string.IsNullOrEmpty(memo)) r.Str(memoTag, memo!);
            if (data is { Length: > 0 }) r.Bytes(dataTag, data!);
            return r.Hash();
        }

        private static byte[] Actual(int vote, string? memo, byte[]? data) =>
            TransactionCodec.ComputeSignatureMetadataHash(
                Pub, Signer, Version, Timestamp, signatureType: Ed25519, vote: vote, memo: memo, data: data);

        [Fact]
        public void NoMemoNoData_MatchesReference_RegressionForExistingPath()
        {
            Assert.Equal(ExpectedHash(0, null, null), Actual(0, null, null));
        }

        [Fact]
        public void SignatureMemo_IsTag9()
        {
            Assert.Equal(ExpectedHash(0, "approved per SOP-7", null),
                         Actual(0, "approved per SOP-7", null));
        }

        [Fact]
        public void SignatureData_IsTag10()
        {
            var data = SHA256.HashData(Encoding.UTF8.GetBytes("qa-report"));
            Assert.Equal(ExpectedHash(0, null, data), Actual(0, null, data));
        }

        [Fact]
        public void Vote_Memo_And_Data_Together()
        {
            var data = SHA256.HashData(Encoding.UTF8.GetBytes("evidence"));
            Assert.Equal(ExpectedHash((int)VoteType.Reject, "rejected: PHI mismatch", data),
                         Actual((int)VoteType.Reject, "rejected: PHI mismatch", data));
        }

        [Fact]
        public void OldOffByOneTags_WouldNotMatch_ProvesBugIsFixed()
        {
            var data = SHA256.HashData(Encoding.UTF8.GetBytes("evidence"));
            var buggy = ExpectedHash(0, "memo", data, buggyTags: true);   // tags 10/11 (the old bug)
            Assert.NotEqual(buggy, Actual(0, "memo", data));              // SDK must NOT match the buggy layout
        }
    }
}
