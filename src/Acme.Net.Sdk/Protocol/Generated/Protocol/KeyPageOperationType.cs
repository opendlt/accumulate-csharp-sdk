namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents the type of key page operation.
    /// Corresponds to JavaScript SDK KeyPageOperationType enum.
    /// </summary>
    public enum KeyPageOperationType
    {
        /// <summary>
        /// Unknown / unset.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Update (replace) a key in the key page.
        /// </summary>
        /// <remarks>
        /// Update is 1 and Remove is 2. These were swapped, which does not fail
        /// loudly: the SDK signs a preimage with the wrong opcode while the node
        /// re-marshals the same JSON body with the right one, so the transaction
        /// hashes differ and the node reports only "transaction is not signed".
        /// Add = 3 was correct, which is why adding keys worked throughout.
        /// </remarks>
        Update = 1,

        /// <summary>
        /// Remove a key from the key page.
        /// </summary>
        Remove = 2,

        /// <summary>
        /// Add a key to the key page.
        /// </summary>
        Add = 3,

        /// <summary>
        /// Set the threshold for the key page.
        /// </summary>
        SetThreshold = 4,

        /// <summary>
        /// Update allowed operations for the key page.
        /// </summary>
        UpdateAllowed = 5,

        /// <summary>
        /// Set the reject threshold for the key page.
        /// </summary>
        SetRejectThreshold = 6,

        /// <summary>
        /// Set the response threshold for the key page.
        /// </summary>
        SetResponseThreshold = 7
    }
}