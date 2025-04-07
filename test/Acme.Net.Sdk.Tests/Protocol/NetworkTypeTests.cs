using Acme.Net.Sdk.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class NetworkTypeTests
    {
        [Fact]
        public void TestIntegerValues()
        {
            Assert.Equal(1, NetworkType.Directory.Value);
            Assert.Equal(2, NetworkType.BlockValidator.Value);
        }

        [Fact]
        public void TestApiNameValues()
        {
            Assert.Equal("directory", NetworkType.Directory.ApiName);
            Assert.Equal("blockValidator", NetworkType.BlockValidator.ApiName);
        }
        
        [Fact]
        public void TestToString()
        {
            Assert.Equal("directory", NetworkType.Directory.ToString());
            Assert.Equal("blockValidator", NetworkType.BlockValidator.ToString());
        }

        [Fact]
        public void TestFromValue()
        {
            Assert.Equal(NetworkType.Directory, NetworkType.FromValue(1));
            Assert.Equal(NetworkType.BlockValidator, NetworkType.FromValue(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => NetworkType.FromValue(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => NetworkType.FromValue(3));
        }

        [Fact]
        public void TestFromApiName()
        {
            Assert.Equal(NetworkType.Directory, NetworkType.FromApiName("directory"));
            Assert.Equal(NetworkType.BlockValidator, NetworkType.FromApiName("blockValidator"));
            Assert.Equal(NetworkType.Directory, NetworkType.FromApiName("DIRECTORY"));
            Assert.Throws<ArgumentException>(() => NetworkType.FromApiName("invalid"));
            Assert.Throws<ArgumentException>(() => NetworkType.FromApiName(null));
            Assert.Throws<ArgumentException>(() => NetworkType.FromApiName(""));
        }

        [Fact]
        public void TestEquality()
        {
            Assert.True(NetworkType.Directory == NetworkType.FromValue(1));
            Assert.True(NetworkType.BlockValidator == NetworkType.FromApiName("blockValidator"));
            Assert.False(NetworkType.Directory == NetworkType.BlockValidator);
            Assert.True(NetworkType.Directory != NetworkType.BlockValidator);
            Assert.True(NetworkType.Directory.Equals(NetworkType.FromApiName("Directory")));
        }
        
        [Fact]
        public void TestJsonSerialization()
        {
            Assert.Equal("\"directory\"", JsonConvert.SerializeObject(NetworkType.Directory));
            Assert.Equal("\"blockValidator\"", JsonConvert.SerializeObject(NetworkType.BlockValidator));
        }
        
        [Fact]
        public void TestJsonDeserialization_FromString()
        {
            Assert.Equal(NetworkType.Directory, JsonConvert.DeserializeObject<NetworkType>("\"directory\""));
            Assert.Equal(NetworkType.BlockValidator, JsonConvert.DeserializeObject<NetworkType>("\"BLOCKVALIDATOR\""));
        }
        
        [Fact]
        public void TestJsonDeserialization_FromInteger()
        {
            Assert.Equal(NetworkType.Directory, JsonConvert.DeserializeObject<NetworkType>("1"));
            Assert.Equal(NetworkType.BlockValidator, JsonConvert.DeserializeObject<NetworkType>("2"));
        }
        
        [Fact]
        public void TestJsonDeserialization_InvalidString()
        {
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NetworkType>("\"invalid\""));
        }
        
        [Fact]
        public void TestJsonDeserialization_InvalidInteger()
        {
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NetworkType>("0"));
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NetworkType>("3"));
        }
        
        [Fact]
        public void TestJsonDeserialization_InvalidType()
        {
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NetworkType>("true"));
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NetworkType>("{}"));
        }
    }
} 