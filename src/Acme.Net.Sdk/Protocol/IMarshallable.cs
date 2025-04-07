using System;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Interface for objects that can be marshalled into a binary format.
    /// Also serves as a marker for RPC bodies.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.protocol.Marshallable.
    /// </summary>
    public interface IMarshallable : IRPCBody
    {
        /// <summary>
        /// Marshals the object into its binary representation.
        /// </summary>
        /// <returns>A byte array representing the marshalled object.</returns>
        byte[] MarshalBinary();
    }
} 