using System;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Interface for enums that provide an explicit integer value.
    /// Corresponds to the Java interface io.accumulatenetwork.sdk.protocol.IntValueEnum.
    /// </summary>
    public interface IIntValueEnum
    {
        /// <summary>
        /// Gets the integer value associated with the enum constant.
        /// </summary>
        int Value { get; }
    }
} 