namespace Acme.Net.Sdk.Protocol.Generated.Protocol
{
    /// <summary>
    /// Represents the type of key page operation.
    /// Corresponds to JavaScript SDK KeyPageOperationType enum.
    /// </summary>
    public enum KeyPageOperationType
    {
        /// <summary>
        /// Add a key to the key page.
        /// </summary>
        Add = 3,

        /// <summary>
        /// Remove a key from the key page.
        /// </summary>
        Remove = 1,

        /// <summary>
        /// Update a key in the key page.
        /// </summary>
        Update = 2,

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