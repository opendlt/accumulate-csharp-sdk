using System;
using Acme.Net.Sdk.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class UrlTests
    {
        [Theory]
        [InlineData("acc://foo/bar", "acc://foo/bar")] // Already correct
        [InlineData("foo/bar", "acc://foo/bar")]       // Adds scheme
        [InlineData("acc://foo", "acc://foo")]         // Authority only
        [InlineData("foo", "acc://foo")]             // Authority only, adds scheme
        [InlineData("acc://foo/bar?q=1#frag", "acc://foo/bar?q=1#frag")] // With query/fragment
        public void TestUrlParsing_Valid(string input, string expectedOutput)
        {
            var url = new Url(input); // Test constructor
            Assert.Equal(expectedOutput, url.String());

            var urlParsed = Url.Parse(input); // Test static Parse
            Assert.Equal(expectedOutput, urlParsed.String());
        }

        [Theory]
        [InlineData(null)] // Null input
        [InlineData("")]   // Empty input
        public void TestUrlParsing_NullOrEmptyThrows(string input)
        {
            Assert.Throws<ArgumentNullException>(() => new Url(input));
            Assert.Throws<ArgumentNullException>(() => Url.Parse(input));
        }
        
        [Theory]
        [InlineData("acc://")]             // Missing authority
        [InlineData("/path/only")]        // Missing authority
        [InlineData("acc:///path/only")] // Missing authority
        [InlineData("://foo/bar")]         // Invalid format (missing scheme)
        [InlineData("http://foo/bar")]   // Valid URI but potentially wrong scheme (ParseInternal allows, constructor/Parse should be ok)
        [InlineData("acc://host:1234/path")] // Valid case (Corrected port)
        public void TestUrlParsing_InvalidOrMissingAuthority(string input)
        {
            if (input == "http://foo/bar" || input == "acc://host:1234/path") { // Corrected port in check
                 // These should parse correctly
                 var url = Url.Parse(input);
                 Assert.NotNull(url);
            } else {
                Assert.Throws<UriFormatException>(() => new Url(input));
                Assert.Throws<UriFormatException>(() => Url.Parse(input));
            }
        }

        [Fact]
        public void TestUrlHelpers()
        {
            var urlString = "acc://foo:bar123@my-company.io:8080/some/path?value=123#section-1";
            var url = Url.Parse(urlString);

            Assert.Equal("my-company.io:8080", url.Authority);
            Assert.Equal("my-company.io", url.HostName);
            Assert.Equal("/some/path", url.Path);
            Assert.Equal("?value=123", url.Query);
            Assert.Equal("#section-1", url.Fragment);

            var root = url.GetRootUrl();
            Assert.Equal("acc://my-company.io:8080", root.String());

            var parent = url.GetParentUrl();
            Assert.Equal("/some", parent.Path);
            Assert.Equal("acc://my-company.io:8080/some", parent.String());

            var parent2 = parent.GetParentUrl();
            Assert.Equal("acc://my-company.io:8080", parent2.String());
            Assert.True(string.IsNullOrEmpty(parent2.Path));
            
            Assert.Throws<InvalidOperationException>(() => parent2.GetParentUrl());
            
            var noPathUrl = Url.Parse("acc://no-path.com");
            Assert.True(string.IsNullOrEmpty(noPathUrl.Path));
            Assert.Throws<InvalidOperationException>(() => noPathUrl.GetParentUrl());
        }
        
        [Fact]
        public void TestPathHelper_TrailingSlash()
        {
            var url = new Url("acc://foo/bar/");
            Assert.Equal("/bar", url.Path);
        }
        
        [Fact]
        public void TestPathHelper_Root()
        {
            var url = new Url("acc://foo/");
            Assert.Equal("", url.Path);
            
            var url2 = new Url("acc://foo");
            Assert.Equal("", url2.Path);
        }

        [Fact]
        public void TestGetRootUrl()
        {
            var url1 = new Url("acc://authority.com:123/path/to/resource?query=val#fragment");
            var expectedRoot1 = new Url("acc://authority.com:123");
            Assert.Equal(expectedRoot1, url1.GetRootUrl());

            var url2 = new Url("acc://foo");
            Assert.Equal(url2, url2.GetRootUrl()); // Root of root is itself
        }

        [Fact]
        public void TestGetParentUrl()
        {
            var url = new Url("acc://host/a/b/c");
            var parent1 = url.GetParentUrl(); // acc://host/a/b
            var parent2 = parent1.GetParentUrl(); // acc://host/a
            var parent3 = parent2.GetParentUrl(); // acc://host

            Assert.Equal(Url.Parse("acc://host/a/b"), parent1);
            Assert.Equal(Url.Parse("acc://host/a"), parent2);
            Assert.Equal(Url.Parse("acc://host"), parent3);

            // Parent of root throws
            Assert.Throws<InvalidOperationException>(() => parent3.GetParentUrl());
        }
        
        [Fact]
        public void TestGetParentUrl_RootWithSlash()
        {
            var url = new Url("acc://host/");
            Assert.Throws<InvalidOperationException>(() => url.GetParentUrl());
        }

        [Fact]
        public void TestEquality()
        {
            var url1a = new Url("acc://foo/bar");
            var url1b = Url.Parse("foo/bar"); // Should resolve to same
            var url2 = new Url("acc://foo/baz");
            Url? urlNull = null;

            Assert.Equal(url1a, url1b);
            Assert.NotEqual(url1a, url2);
            Assert.False(url1a.Equals(urlNull));
            Assert.False(url1a.Equals(new object()));
            Assert.Equal(url1a.GetHashCode(), url1b.GetHashCode());
            Assert.NotEqual(url1a.GetHashCode(), url2.GetHashCode());
        }

        [Fact]
        public void TestToStringMethod()
        {
             var url = new Url("acc://foo/bar?q=1");
             Assert.Equal("acc://foo/bar?q=1", url.ToString());
        }
        
        [Fact]
        public void TestJsonSerialization()
        {
            var url = new Url("acc://some/path");
            string expectedJson = "\"acc://some/path\""; // Serializes as string
            string actualJson = JsonConvert.SerializeObject(url);
            Assert.Equal(expectedJson, actualJson);
        }
        
        [Fact]
        public void TestJsonSerialization_Null()
        {
            Url? url = null;
            string expectedJson = "null";
            string actualJson = JsonConvert.SerializeObject(url);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void TestJsonDeserialization()
        {
            string json = "\"acc://test/deserialize?a=b\"";
            var expectedUrl = new Url("acc://test/deserialize?a=b");
            var actualUrl = JsonConvert.DeserializeObject<Url>(json);
            Assert.Equal(expectedUrl, actualUrl);
        }
        
        [Fact]
        public void TestJsonDeserialization_NoScheme()
        {
            string json = "\"myhost/mypath\""; // Missing scheme
            var expectedUrl = new Url("acc://myhost/mypath");
            var actualUrl = JsonConvert.DeserializeObject<Url>(json);
            Assert.Equal(expectedUrl, actualUrl);
        }

        [Fact]
        public void TestJsonDeserialization_InvalidStringThrows()
        {
            string json = "\"://invalid\"";
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Url>(json));
        }
        
        [Fact]
        public void TestJsonDeserialization_NonStringThrows()
        {
            string json = "123";
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Url>(json));
            
             string json2 = "{}";
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Url>(json2));
        }
        
        [Fact]
        public void TestJsonDeserialization_Null()
        {
             string json = "null";
             var actualUrl = JsonConvert.DeserializeObject<Url>(json);
             Assert.Null(actualUrl);
        }
    }
} 