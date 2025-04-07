using System;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Commons.Codec.Binary; // For Hex
using Newtonsoft.Json;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class TxIDTests
    {
        // Example valid TxID components
        private const string ValidHashHex = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        private static readonly byte[] ValidHashBytes = Convert.FromHexString(ValidHashHex);
        private const string ValidAccountUrlStr = "acc://bunch-of-characters.acme";
        private static readonly Url ValidAccountUrl = Url.Parse(ValidAccountUrlStr);
        private static readonly string ValidTxIdUrlStr = $"acc://{ValidHashHex}@{ValidAccountUrl.Authority}{ValidAccountUrl.Path}";
        private static readonly Url ValidTxIdUrl = Url.Parse(ValidTxIdUrlStr);

        [Fact]
        public void TestConstruction_FromUrl()
        {
            var txid = new TxID(ValidTxIdUrl);
            Assert.Equal(ValidTxIdUrl, txid.GetUrl());
        }

        [Fact]
        public void TestConstruction_FromString()
        {
            var txid = new TxID(ValidTxIdUrlStr);
            Assert.Equal(ValidTxIdUrl, txid.GetUrl()); // Url.Parse should create equivalent Url
            Assert.Equal(ValidTxIdUrlStr, txid.ToString());
        }

        [Fact]
        public void TestConstruction_NullOrEmptyString_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TxID((string)null!));
            Assert.Throws<ArgumentNullException>(() => new TxID(""));
        }
        
        [Fact]
        public void TestConstruction_InvalidUrlString_ThrowsUriFormat()
        {
            Assert.Throws<UriFormatException>(() => new TxID("acc:invalid"));
        }

        [Fact]
        public void TestConstruction_NullUrl_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TxID((Url)null!));
        }

        [Fact]
        public void TestGetHash_ValidUrl_ExtractsCorrectly()
        {
            var txid = new TxID(ValidTxIdUrl);
            Assert.Equal(ValidHashBytes, txid.GetHash());
            // Call again to ensure cached value is returned correctly
            Assert.Equal(ValidHashBytes, txid.GetHash()); 
        }

        [Theory]
        [InlineData("acc://nohash@domain.com")] // Missing hash, but technically valid URI
        [InlineData("acc://@domain.com")] // Empty hash
        [InlineData("acc:/abcdef@domain.com")] // Missing //
        [InlineData("acc://abcdefdomain.com")] // Missing @
        [InlineData("acc://invalid-hex-chars@domain.com")] // Invalid hex
        [InlineData("just-plain-text")] // Completely invalid format
        public void TestGetHash_InvalidUrlFormat_ThrowsFormat(string invalidUrl)
        {
            // Must be a valid URI for Url.Parse to succeed first
            try {
                 var txid = new TxID(Url.Parse(invalidUrl));
                 Assert.Throws<FormatException>(() => txid.GetHash());
            } catch (UriFormatException) {
                // If Url.Parse itself fails, that's expected for some inputs
                Assert.True(true); 
            }
        }

        [Fact]
        public void TestEqualsAndHashCode()
        {
            var txid1 = new TxID(ValidTxIdUrlStr);
            var txid2 = new TxID(ValidTxIdUrl);
            var txid3 = new TxID($"acc://{ValidHashHex.ToUpper()}@{ValidAccountUrl.Authority}{ValidAccountUrl.Path}"); // Same hash, different case
            var txid_differentHash = new TxID($"acc://001122@{ValidAccountUrl.Authority}{ValidAccountUrl.Path}");
            var txid_differentAccount = new TxID($"acc://{ValidHashHex}@different-account.acme");

            // Reflexive
            Assert.True(txid1.Equals(txid1));

            // Symmetric & Equal
            Assert.True(txid1.Equals(txid2));
            Assert.True(txid2.Equals(txid1));
            Assert.True(txid1 == txid2);
            Assert.False(txid1 != txid2);
            Assert.Equal(txid1.GetHashCode(), txid2.GetHashCode());

            // Equals despite hex case difference (Url equality handles authority case, hash equality is byte-based)
            Assert.True(txid1.Equals(txid3));
            Assert.True(txid3.Equals(txid1));
            Assert.True(txid1 == txid3);
             Assert.Equal(txid1.GetHashCode(), txid3.GetHashCode());

            // Unequal - Different Hash
            Assert.False(txid1.Equals(txid_differentHash));
            Assert.False(txid_differentHash.Equals(txid1));
            Assert.True(txid1 != txid_differentHash);

             // Unequal - Different Account URL
            Assert.False(txid1.Equals(txid_differentAccount));
            Assert.False(txid_differentAccount.Equals(txid1));
            Assert.True(txid1 != txid_differentAccount);

            // Null comparison
            Assert.False(txid1.Equals(null));
            Assert.False(txid1 == null);
            Assert.True(txid1 != null);
            TxID? nullTxid = null;
            Assert.True(nullTxid == null);
            Assert.False(nullTxid != null);
        }

        [Fact]
        public void TestToString()
        {
            var txid = new TxID(ValidTxIdUrlStr);
            // Url.String() might slightly modify (e.g. trailing slash), so compare via Url object
            Assert.Equal(ValidTxIdUrl.String(), txid.ToString());
        }

        // --- JSON Serialization Tests ---

        private class TestTxIDContainer
        {
            public TxID? TransactionId { get; set; }
            public TxID? NullTransactionId { get; set; }
        }

        [Fact]
        public void TestJsonSerialization_SerializesAsString()
        {
            var container = new TestTxIDContainer
            {
                TransactionId = new TxID(ValidTxIdUrlStr),
                NullTransactionId = null
            };

            string expectedJson = $"{{\"TransactionId\":\"{ValidTxIdUrl.String()}\",\"NullTransactionId\":null}}";
            string actualJson = JsonConvert.SerializeObject(container);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void TestJsonDeserialization_DeserializesFromString()
        {
            string json = $"{{\"TransactionId\":\"{ValidTxIdUrlStr}\",\"NullTransactionId\":null}}";
            var container = JsonConvert.DeserializeObject<TestTxIDContainer>(json);

            Assert.NotNull(container);
            Assert.NotNull(container.TransactionId);
            Assert.Null(container.NullTransactionId);

            // Check if the deserialized TxID is correct
            Assert.Equal(ValidTxIdUrl, container.TransactionId.GetUrl());
            Assert.Equal(ValidHashBytes, container.TransactionId.GetHash()); // Verify hash extraction works after deserialization
        }

        [Theory]
        // Valid JSON, but invalid data type/value for TxID property
        [InlineData("{ \"TransactionId\": 123 }")]          // Invalid token type (number)
        [InlineData("{ \"TransactionId\": true }")]         // Invalid token type (boolean)
        [InlineData("{ \"TransactionId\": [] }")]          // Invalid token type (array)
        [InlineData("{ \"TransactionId\": {} }")]          // Invalid token type (object)
        [InlineData("{ \"TransactionId\": \"\" }")]          // Empty string (rejected by TxIDConverter)
        [InlineData("{ \"TransactionId\": \"not a url\" }")] // Invalid URL format string (rejected by TxID constructor via Url.Parse)
        public void TestJsonDeserialization_InvalidInput_ThrowsJsonSerializationException(string invalidJson)
        {
            // Expect JsonSerializationException either from TxIDConverter or from exceptions during TxID construction
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<TestTxIDContainer>(invalidJson));
        }

        [Fact]
        public void TestJsonDeserialization_NullStringValue()
        {
            string json = "{\"TransactionId\":null}";
            var container = JsonConvert.DeserializeObject<TestTxIDContainer>(json);
             Assert.NotNull(container);
            Assert.Null(container.TransactionId);
        }
    }
}
