using System;
using System.Text;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Support;
using Xunit;

namespace Acme.Net.Sdk.Tests.Protocol
{
    public class TransactionHasherTests
    {
        [Fact]
        public void ComputeTransactionHash_WithNullTransaction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TransactionHasher.ComputeTransactionHash(null));
        }

        [Fact]
        public void ComputeTransactionHash_WithNullHeader_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = null,
                Body = new WriteData()
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TransactionHasher.ComputeTransactionHash(transaction));
        }

        [Fact]
        public void ComputeTransactionHash_WithNullBody_ThrowsArgumentNullException()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader(),
                Body = null
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TransactionHasher.ComputeTransactionHash(transaction));
        }

        [Fact]
        public void ComputeTransactionHash_WithValidTransaction_ReturnsHash()
        {
            // Arrange
            var transaction = CreateSampleTransaction();

            // Act
            byte[] hash = TransactionHasher.ComputeTransactionHash(transaction);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length); // SHA-256 hash length
        }

        [Fact]
        public void HashTransaction_SetsHashOnTransaction()
        {
            // Arrange
            var transaction = CreateSampleTransaction();
            Assert.Null(transaction.Hash); // Verify hash is initially null

            // Act
            var result = TransactionHasher.HashTransaction(transaction);

            // Assert
            Assert.Same(transaction, result); // Should return the same transaction instance
            Assert.NotNull(transaction.Hash); // Hash should be set
            Assert.Equal(32, transaction.Hash.Length); // SHA-256 hash
        }

        [Fact]
        public void HashTransaction_WithDifferentHeaders_ProducesDifferentHashes()
        {
            // Arrange
            var transaction1 = CreateSampleTransaction();
            var transaction2 = CreateSampleTransaction();
            transaction2.Header.WithMemo("Different memo");

            // Act
            TransactionHasher.HashTransaction(transaction1);
            TransactionHasher.HashTransaction(transaction2);

            // Assert
            Assert.NotEqual(transaction1.Hash, transaction2.Hash);
        }

        [Fact]
        public void HashTransaction_WithDifferentBodies_ProducesDifferentHashes()
        {
            // Arrange
            var transaction1 = CreateSampleTransaction();
            var transaction2 = CreateSampleTransaction();
            ((WriteData)transaction2.Body).WithData("Different data");

            // Act
            TransactionHasher.HashTransaction(transaction1);
            TransactionHasher.HashTransaction(transaction2);

            // Assert
            Assert.NotEqual(transaction1.Hash, transaction2.Hash);
        }

        [Fact]
        public void HashTransaction_WithSpecialWriteDataBody_HandlesCorrectly()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://test.acme"))
                    .WithMemo("Test Memo"),
                Body = new WriteData()
                    .WithData("Test Data")
                    .WithFormat("json")
                    .WithEntryHash("abcdef1234567890")
            };

            // Act
            TransactionHasher.HashTransaction(transaction);

            // Assert
            Assert.NotNull(transaction.Hash);
            Assert.Equal(32, transaction.Hash.Length);
        }

        [Fact]
        public void HashTransaction_WithSpecialWriteDataToBody_HandlesCorrectly()
        {
            // Arrange
            var transaction = new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://test.acme"))
                    .WithMemo("Test Memo"),
                Body = new WriteDataTo()
                    .WithRecipient(new Url("acc://recipient.acme"))
                    .WithData("Test Data")
                    .WithFormat("json")
                    .WithEntryHash("abcdef1234567890")
            };

            // Act
            TransactionHasher.HashTransaction(transaction);

            // Assert
            Assert.NotNull(transaction.Hash);
            Assert.Equal(32, transaction.Hash.Length);
        }

        private Transaction CreateSampleTransaction()
        {
            return new Transaction
            {
                Header = new TransactionHeader()
                    .WithPrincipal(new Url("acc://test.acme"))
                    .WithMemo("Test Transaction"),
                    
                Body = new WriteData()
                    .WithData(Encoding.UTF8.GetBytes("Hello, World!"))
            };
        }
    }
} 