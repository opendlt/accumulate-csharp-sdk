using Acme.Net.Sdk.Protocol.Generated;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Pins <see cref="SignatureType"/> to the protocol's wire values.
    ///
    /// <para>
    /// Every value below is transcribed from Go core <c>protocol/enums_gen.go</c>
    /// (<c>SignatureTypeXxx</c> constants). They are consensus-critical: the type is marshalled as
    /// field 1 of the signature metadata, so it feeds the metadata hash, the header initiator and
    /// the signing preimage. A wrong value yields a well-formed signature the node rejects with
    /// "transaction is not signed", with nothing in the crypto or the logs to point at the enum.
    /// </para>
    /// <para>
    /// This regression exists: values 4 and up were once assigned in declaration order rather than
    /// taken from the protocol, which silently broke rsaSha256, ecdsaSha256, btc, eth, delegated,
    /// typedData and authority. Nobody noticed because only the Ed25519 path was ever exercised,
    /// and its values happened to be right.
    /// </para>
    /// </summary>
    public class SignatureTypeValueTests
    {
        [Theory]
        [InlineData(SignatureType.UNKNOWN, 0, "unknown")]
        [InlineData(SignatureType.LEGACY_ED25519, 1, "legacyED25519")]
        [InlineData(SignatureType.ED25519, 2, "ed25519")]
        [InlineData(SignatureType.RCD1, 3, "rcd1")]
        [InlineData(SignatureType.RECEIPT, 4, "receipt")]
        [InlineData(SignatureType.PARTITION, 5, "partition")]
        [InlineData(SignatureType.SET, 6, "set")]
        [InlineData(SignatureType.REMOTE, 7, "remote")]
        [InlineData(SignatureType.BTC, 8, "btc")]
        [InlineData(SignatureType.BTC_LEGACY, 9, "btcLegacy")]
        [InlineData(SignatureType.ETH, 10, "eth")]
        [InlineData(SignatureType.DELEGATED, 11, "delegated")]
        [InlineData(SignatureType.INTERNAL, 12, "internal")]
        [InlineData(SignatureType.AUTHORITY, 13, "authority")]
        [InlineData(SignatureType.RSA_SHA256, 14, "rsaSha256")]
        [InlineData(SignatureType.ECDSA_SHA256, 15, "ecdsaSha256")]
        [InlineData(SignatureType.TYPED_DATA, 16, "typedData")]
        public void MatchesTheProtocolWireValueAndName(SignatureType type, int wireValue, string wireName)
        {
            Assert.Equal(wireValue, (int)type);
            Assert.Equal(wireName, type.GetWireName());
            Assert.Equal(type, SignatureTypeExtensions.FromValue(wireValue));
            Assert.Equal(type, SignatureTypeExtensions.FromWireName(wireName));
        }

        /// <summary>
        /// The two the PKI work turns on, called out separately: these are the values whose being
        /// wrong is unrecoverable at run time, and they were 9 and 10 before this was fixed.
        /// </summary>
        [Fact]
        public void RsaAndEcdsaAreFourteenAndFifteen()
        {
            Assert.Equal(14, (int)SignatureType.RSA_SHA256);
            Assert.Equal(15, (int)SignatureType.ECDSA_SHA256);
        }

        /// <summary>
        /// No two members may share a value: the reverse lookup is a dictionary keyed on the
        /// integer, so a collision would silently resolve to whichever member was declared last.
        /// </summary>
        [Fact]
        public void EveryValueIsDistinct()
        {
            var values = Enum.GetValues<SignatureType>().Select(t => (int)t).ToArray();
            Assert.Equal(values.Length, values.Distinct().Count());
        }

        /// <summary>
        /// An unrecognised value degrades to UNKNOWN rather than throwing or inventing a member —
        /// this is what keeps a node newer than the SDK from crashing a client.
        /// </summary>
        [Fact]
        public void UnrecognisedValuesAndNamesBecomeUnknown()
        {
            Assert.Equal(SignatureType.UNKNOWN, SignatureTypeExtensions.FromValue(99));
            Assert.Equal(SignatureType.UNKNOWN, SignatureTypeExtensions.FromWireName("nonesuch"));
            Assert.Equal(SignatureType.UNKNOWN, SignatureTypeExtensions.FromWireName(null!));
        }
    }
}
