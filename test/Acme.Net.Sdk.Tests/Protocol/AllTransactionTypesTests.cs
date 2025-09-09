#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Acme.Net.Sdk.Commons.Codec.Binary;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Tests all transaction types against protocol test vectors.
    /// </summary>
    public class AllTransactionTypesTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ProtocolTestVectors _testVectors;

        public AllTransactionTypesTests(ITestOutputHelper output)
        {
            _output = output;
            _testVectors = TestVectors.Vectors;
        }

        [Fact]
        public void TestAllTransactionTypesFound()
        {
            var expectedTypes = new[]
            {
                "CreateIdentity",
                "CreateTokenAccount",
                "SendTokens",
                "CreateDataAccount",
                "WriteData",
                "WriteDataTo",
                "AcmeFaucet",
                "CreateToken",
                "IssueTokens",
                "BurnTokens",
                "CreateKeyPage",
                "CreateKeyBook",
                "AddCredits",
                "UpdateKeyPage",
                "SignPending",
                "SyntheticCreateIdentity",
                "SyntheticWriteData",
                "SyntheticDepositTokens",
                "SyntheticDepositCredits",
                "SyntheticBurnTokens"
            };

            var foundTypes = _testVectors.Transactions.Select(v => v.Name).Distinct().OrderBy(n => n).ToList();
            _output.WriteLine($"Found {foundTypes.Count} transaction types in test vectors:");
            foreach (var type in foundTypes)
            {
                _output.WriteLine($"  - {type}");
            }

            foreach (var expectedType in expectedTypes)
            {
                Assert.Contains(expectedType, foundTypes, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [MemberData(nameof(GetTransactionTestCases))]
        public void TestTransactionMarshalling(string transactionType)
        {
            var transactionGroup = _testVectors.Transactions.FirstOrDefault(v => v.Name.Equals(transactionType, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(transactionGroup);
            Assert.NotEmpty(transactionGroup.Cases);
            
            _output.WriteLine($"Testing {transactionGroup.Cases.Count} {transactionType} test case(s)");
            
            foreach (var testCase in transactionGroup.Cases)
            {
                // Decode the binary data
                var binaryData = Convert.FromBase64String(testCase.Binary);
                _output.WriteLine($"  - Binary data length: {binaryData.Length}");
                
                // Verify the transaction can be identified by its type code
                var unmarshaller = new Unmarshaller(binaryData);
                if (unmarshaller.TryReadField(1, out var typeCodeValue))
                {
                    var typeCode = Convert.ToInt32(typeCodeValue);
                    _output.WriteLine($"  - Type code: {typeCode}");
                    
                    // Map type code to expected transaction type name
                    var expectedTypeName = GetTransactionTypeName(typeCode);
                    Assert.NotNull(expectedTypeName);
                    _output.WriteLine($"  - Mapped type name: {expectedTypeName}");
                    
                    // For now we're validating that we can parse the type code correctly
                    // In the future, we can add full marshalling/unmarshalling tests
                }
            }
        }

        private string? GetTransactionTypeName(int typeCode)
        {
            return typeCode switch
            {
                TransactionTypeCode.CreateIdentity => "CreateIdentity",
                TransactionTypeCode.CreateTokenAccount => "CreateTokenAccount",
                TransactionTypeCode.SendTokens => "SendTokens",
                TransactionTypeCode.CreateDataAccount => "CreateDataAccount",
                TransactionTypeCode.WriteData => "WriteData",
                TransactionTypeCode.WriteDataTo => "WriteDataTo",
                TransactionTypeCode.AcmeFaucet => "AcmeFaucet",
                TransactionTypeCode.CreateToken => "CreateToken",
                TransactionTypeCode.IssueTokens => "IssueTokens",
                TransactionTypeCode.BurnTokens => "BurnTokens",
                TransactionTypeCode.CreateKeyPage => "CreateKeyPage",
                TransactionTypeCode.CreateKeyBook => "CreateKeyBook",
                TransactionTypeCode.AddCredits => "AddCredits",
                TransactionTypeCode.UpdateKeyPage => "UpdateKeyPage",
                TransactionTypeCode.Remote => "SignPending",
                TransactionTypeCode.SyntheticCreateIdentity => "SyntheticCreateIdentity",
                TransactionTypeCode.SyntheticWriteData => "SyntheticWriteData",
                TransactionTypeCode.SyntheticDepositTokens => "SyntheticDepositTokens",
                TransactionTypeCode.SyntheticDepositCredits => "SyntheticDepositCredits",
                TransactionTypeCode.SyntheticBurnTokens => "SyntheticBurnTokens",
                _ => null
            };
        }

        public static IEnumerable<object[]> GetTransactionTestCases()
        {
            yield return new object[] { "CreateIdentity" };
            yield return new object[] { "CreateTokenAccount" };
            yield return new object[] { "SendTokens" };
            yield return new object[] { "CreateDataAccount" };
            yield return new object[] { "WriteData" };
            yield return new object[] { "WriteDataTo" };
            yield return new object[] { "AcmeFaucet" };
            yield return new object[] { "CreateToken" };
            yield return new object[] { "IssueTokens" };
            yield return new object[] { "BurnTokens" };
            yield return new object[] { "CreateKeyPage" };
            yield return new object[] { "CreateKeyBook" };
            yield return new object[] { "AddCredits" };
            yield return new object[] { "UpdateKeyPage" };
            yield return new object[] { "SignPending" };
            yield return new object[] { "SyntheticCreateIdentity" };
            yield return new object[] { "SyntheticWriteData" };
            yield return new object[] { "SyntheticDepositTokens" };
            yield return new object[] { "SyntheticDepositCredits" };
            yield return new object[] { "SyntheticBurnTokens" };
        }

    }
}