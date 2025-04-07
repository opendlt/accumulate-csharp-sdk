using System;
using System.Collections.Generic;
// using Acme.Net.Sdk.Core; // Assuming ITransaction and Signature will be here or referenced - REMOVED

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Builder for transaction envelopes.
    /// This is a placeholder implementation that will be expanded later.
    /// </summary>
    public class EnvelopeBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvelopeBuilder"/> class.
        /// </summary>
        public EnvelopeBuilder()
        {
        }

        private readonly List<ITransaction> _transactions = new List<ITransaction>();
        private readonly List<Signature> _signatures = new List<Signature>();
        private string? _txHash;

        /// <summary>
        /// Adds a transaction to the envelope.
        /// </summary>
        /// <param name="transaction">The transaction to add.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the transaction is null.</exception>
        public EnvelopeBuilder AddTransaction(ITransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            _transactions.Add(transaction);
            return this;
        }

        /// <summary>
        /// Adds a signature to the envelope.
        /// </summary>
        /// <param name="signature">The signature to add.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the signature is null.</exception>
        public EnvelopeBuilder AddSignature(Signature signature)
        {
             if (signature == null) throw new ArgumentNullException(nameof(signature));
           _signatures.Add(signature);
            return this;
        }

        /// <summary>
        /// Sets the transaction hash for the envelope.
        /// </summary>
        /// <param name="txHash">The transaction hash string.</param>
        /// <returns>The builder instance for chaining.</returns>
        public EnvelopeBuilder SetTxHash(string? txHash)
        {
            _txHash = txHash;
            return this;
        }

        /// <summary>
        /// Builds the Envelope object.
        /// Note: The Envelope class itself needs to be defined.
        /// </summary>
        /// <returns>A new Envelope instance.</returns>
        public Envelope Build()
        {
            // TODO: Replace with actual Envelope constructor call once Envelope is defined.
            // Example placeholder:
             return new Envelope(_transactions, _signatures, _txHash);
        }
    }

    // Placeholder definitions - these will need proper implementation later
    // It's often better to define these in their own files when fully implemented.
    public interface ITransaction { /* ... */ }
    public class Signature { /* ... */ }
    public class Envelope 
    {
        public IReadOnlyList<ITransaction> Transactions { get; }
        public IReadOnlyList<Signature> Signatures { get; }
        public string? TxHash { get; }

        // Constructor matching the Build method's call
        public Envelope(List<ITransaction> transactions, List<Signature> signatures, string? txHash)
        { 
            // Defensive copies are good practice
            Transactions = new List<ITransaction>(transactions).AsReadOnly(); 
            Signatures = new List<Signature>(signatures).AsReadOnly();
            TxHash = txHash;
        }
    }
}
