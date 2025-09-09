using System;
using Xunit;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;

namespace Acme.Net.Sdk.Tests.Protocol
{
    /// <summary>
    /// Tests for error handling in TransactionHasher.
    /// </summary>
    public class TransactionHasherErrorTests
    {
        [Fact]
        public void ComputeTransactionHash_NullTransaction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.ComputeTransactionHash(null));
            Assert.Equal("transaction", ex.ParamName);
        }

        [Fact]
        public void ComputeTransactionHash_NullHeader_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = null,
                Body = new AddCredits()
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.ComputeTransactionHash(transaction));
            Assert.Contains("Header", ex.Message);
        }

        [Fact]
        public void ComputeTransactionHash_NullBody_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader(),
                Body = null
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.ComputeTransactionHash(transaction));
            Assert.Contains("Body", ex.Message);
        }

        [Fact]
        public void ComputeRawHash_NullTransaction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.ComputeRawHash(null));
            Assert.Equal("transaction", ex.ParamName);
        }

        [Fact]
        public void ComputeRawHash_NullHeader_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = null,
                Body = new SendTokens()
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.ComputeRawHash(transaction));
            Assert.Contains("header", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ComputeRawHash_NullBody_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader(),
                Body = null
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.ComputeRawHash(transaction));
            Assert.Contains("body", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void HashTransaction_NullTransaction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.HashTransaction(null));
            Assert.Equal("transaction", ex.ParamName);
        }

        [Fact]
        public void HashTransaction_NullHeader_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = null,
                Body = new CreateToken()
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.HashTransaction(transaction));
            Assert.Contains("Header", ex.Message);
        }

        [Fact]
        public void HashTransaction_NullBody_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader(),
                Body = null
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                TransactionHasher.HashTransaction(transaction));
            Assert.Contains("Body", ex.Message);
        }

        [Fact]
        public void ComputeTransactionHash_WithReferenceHash_UsesReferenceHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader(),
                Body = new AddCredits()
            };
            var referenceHash = new byte[32];
            for (int i = 0; i < 32; i++) referenceHash[i] = (byte)i;

            // Act
            var result = TransactionHasher.ComputeTransactionHash(transaction, referenceHash);

            // Assert
            Assert.Equal(referenceHash, result);
        }

        [Fact]
        public void ComputeTransactionHash_WithInvalidReferenceHashLength_ComputesHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/tokens"),
                Body = new SendTokens()
                    .AddRecipient("acc://bob/tokens", 100)
            };
            var invalidReferenceHash = new byte[16]; // Wrong length

            // Act
            var result = TransactionHasher.ComputeTransactionHash(transaction, invalidReferenceHash);

            // Assert
            Assert.NotEqual(invalidReferenceHash, result);
            Assert.Equal(32, result.Length);
        }

        [Fact]
        public void ComputeTransactionHash_WithExistingHash_UsesExistingHash()
        {
            // Arrange
            var existingHash = new byte[32];
            for (int i = 0; i < 32; i++) existingHash[i] = (byte)(31 - i);
            
            var transaction = new Transaction
            {
                Header = new TransactionHeader(),
                Body = new AddCredits(),
                Hash = existingHash
            };

            // Act
            var result = TransactionHasher.ComputeTransactionHash(transaction);

            // Assert
            Assert.Equal(existingHash, result);
        }

        [Fact]
        public void WriteData_WithNullDataEntry_ComputesValidHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/data"),
                Body = new WriteData
                {
                    DataEntry = null
                }
            };

            // Act
            var hash = TransactionHasher.ComputeRawHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        [Fact]
        public void WriteDataTo_WithNullDataEntry_ComputesValidHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/data"),
                Body = new WriteDataTo
                {
                    Recipient = new Url("acc://bob/data"),
                    DataEntry = null
                }
            };

            // Act
            var hash = TransactionHasher.ComputeRawHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        [Fact]
        public void AccumulateDataEntry_WithEmptyData_ComputesValidHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/data"),
                Body = new WriteData
                {
                    DataEntry = new AccumulateDataEntry
                    {
                        Data = new byte[0][]
                    }
                }
            };

            // Act
            var hash = TransactionHasher.ComputeRawHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        [Fact]
        public void AccumulateDataEntry_WithNullDataElements_ComputesValidHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/data"),
                Body = new WriteData
                {
                    DataEntry = new AccumulateDataEntry
                    {
                        Data = new byte[][] { null, new byte[] { 1, 2, 3 }, null }
                    }
                }
            };

            // Act
            var hash = TransactionHasher.ComputeRawHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        [Fact]
        public void DoubleHashDataEntry_WithEmptyData_ComputesValidHash()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/data"),
                Body = new WriteData
                {
                    DataEntry = new DoubleHashDataEntry
                    {
                        Data = new byte[0][]
                    }
                }
            };

            // Act
            var hash = TransactionHasher.ComputeRawHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        [Fact]
        public void UnknownDataEntryType_UsesStandardHashing()
        {
            // Arrange
            var customEntry = new CustomDataEntry();
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal("acc://alice/data"),
                Body = new WriteData
                {
                    DataEntry = customEntry
                }
            };

            // Act
            var hash = TransactionHasher.ComputeRawHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        private class CustomDataEntry : Acme.Net.Sdk.Protocol.Generated.Protocol.IDataEntry
        {
            public DataEntryType EntryType => DataEntryType.Unknown;
            
            public byte[] MarshalBinary()
            {
                return new byte[] { 0x01, 0x02, 0x03 };
            }
        }
    }
}