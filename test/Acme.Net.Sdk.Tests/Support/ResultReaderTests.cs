using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Acme.Net.Sdk.Support;
using Xunit;

namespace Acme.Net.Sdk.Tests.Support
{
    // Simple class for testing deserialization
    public class TestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public bool Active { get; set; }

        // For comparison
        public override bool Equals(object? obj)
        {
            return obj is TestDto dto &&
                   Name == dto.Name &&
                   Value == dto.Value &&
                   Active == dto.Active;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Value, Active);
        }
    }

    public class ResultReaderTests
    {
        [Fact]
        public void TestReadValue_String_Generic()
        {
            string json = "{\"name\":\"Test\", \"value\":123, \"active\":true}";
            var expected = new TestDto { Name = "Test", Value = 123, Active = true };
            var actual = ResultReader.ReadValue<TestDto>(json);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestReadValue_String_TypeParam()
        {
            string json = "{\"name\":\"Test\", \"value\":123, \"active\":true}";
            var expected = new TestDto { Name = "Test", Value = 123, Active = true };
            var actual = (TestDto)ResultReader.ReadValue(json, typeof(TestDto));
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void TestReadValue_String_InvalidJsonThrows()
        {
             string invalidJson = "{\"name\":\"Test\""; // Missing closing brace
             // Update expected exception type based on actual behavior
            Assert.Throws<JsonSerializationException>(() => ResultReader.ReadValue<TestDto>(invalidJson));
        }

        [Fact]
        public void TestReadValue_JToken_Generic()
        {
            var jobject = JObject.Parse("{\"name\":\"TokenTest\", \"value\":456, \"active\":false}");
            var expected = new TestDto { Name = "TokenTest", Value = 456, Active = false };
            var actual = ResultReader.ReadValue<TestDto>(jobject);
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void TestReadValue_JToken_TypeParam()
        {
             var jobject = JObject.Parse("{\"name\":\"TokenTest\", \"value\":456, \"active\":false}");
            var expected = new TestDto { Name = "TokenTest", Value = 456, Active = false };
            var actual = (TestDto)ResultReader.ReadValue(jobject, typeof(TestDto));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestReadValue_JToken_InvalidTokenThrows()
        {
            var jValue = new JValue("not an object");
            // Expecting JsonSerializationException when trying to convert incompatible type
            Assert.Throws<JsonSerializationException>(() => ResultReader.ReadValue<TestDto>(jValue)); 
        }

        [Fact]
        public void TestReadList_JToken()
        {
            var jarray = JArray.Parse("[{\"name\":\"Item1\", \"value\":1}, {\"name\":\"Item2\", \"value\":2}]" );
            var expected = new List<TestDto>
            {
                new TestDto { Name = "Item1", Value = 1, Active = false }, // Active defaults to false
                new TestDto { Name = "Item2", Value = 2, Active = false }
            };
            var actual = ResultReader.ReadList<TestDto>(jarray);
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void TestReadList_JToken_InvalidTokenThrows()
        {
             var jobject = JObject.Parse("{\"name\":\"Item1\", \"value\":1}"); // Not an array
            Assert.Throws<JsonSerializationException>(() => ResultReader.ReadList<TestDto>(jobject));
        }

        // Test stubbed methods
        [Fact]
        public void TestStubbedMethodsThrowNotImplemented()
        {
            var dummyToken = new JObject(); // Dummy token for methods needing it
            var dummyResponse = new object(); // Dummy object
            var dummyStatus = new object();

            Assert.Throws<NotImplementedException>(() => ResultReader.ReadMultiResponse<object>(dummyToken, typeof(object)));
            Assert.Throws<NotImplementedException>(() => ResultReader.CheckForErrors(dummyResponse));
            Assert.Throws<NotImplementedException>(() => ResultReader.CheckForErrors(dummyResponse, dummyStatus));
            Assert.Throws<NotImplementedException>(() => ResultReader.GetTransactionType(dummyToken));
            Assert.Throws<NotImplementedException>(() => ResultReader.GetAccountType(dummyToken));
        }
    }
} 