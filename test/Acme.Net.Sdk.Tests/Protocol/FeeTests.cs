using System.Numerics;
using Acme.Net.Sdk.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class FeeTests
    {
        [Fact]
        public void TestFeeConstructorAndValue()
        {
            var feeValue = new BigInteger(12345);
            var fee = new Fee(feeValue);
            Assert.Equal(feeValue, fee.Value);
            Assert.Equal(feeValue, (BigInteger)fee); // Test implicit conversion
        }

        [Fact]
        public void TestFeeDefaultConstructor()
        {
            var fee = new Fee();
            Assert.Equal(BigInteger.Zero, fee.Value);
        }

        [Fact]
        public void TestFeeEquality()
        {
            var fee1 = new Fee(100);
            var fee2 = new Fee(100);
            var fee3 = new Fee(200);
            Fee? feeNull = null;

            Assert.Equal(fee1, fee2);
            Assert.NotEqual(fee1, fee3);
            Assert.False(fee1.Equals(feeNull));
            Assert.False(fee1.Equals(new object()));
            Assert.Equal(fee1.GetHashCode(), fee2.GetHashCode());
        }

        [Fact]
        public void TestFeeToString()
        {
             var fee = new Fee(9876);
             Assert.Equal("9876", fee.ToString());
        }

        [Fact]
        public void TestFeeSerialization_Unwrapped()
        {
            var fee = new Fee(123456789012345);
            string expectedJson = "123456789012345"; // Value directly, not nested
            string actualJson = JsonConvert.SerializeObject(fee);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void TestFeeDeserialization_Unwrapped()
        {
             string json = "98765432109876";
             var expectedFee = new Fee(BigInteger.Parse(json));
             var actualFee = JsonConvert.DeserializeObject<Fee>(json);
             Assert.Equal(expectedFee, actualFee);
        }
        
        [Fact]
        public void TestFeeDeserialization_Null()
        {
             string json = "null";
             var actualFee = JsonConvert.DeserializeObject<Fee>(json);
             Assert.Null(actualFee);
        }
        
         [Fact]
        public void TestFeeSerialization_Null()
        {
            Fee? fee = null;
            string expectedJson = "null";
            string actualJson = JsonConvert.SerializeObject(fee);
            Assert.Equal(expectedJson, actualJson);
        }
    }
} 