namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Specifies the hashing mode to use for initialization.
    /// </summary>
    public enum InitHashMode
    {
        /// <summary>
        /// Initialize using a Merkle hash.
        /// </summary>
        InitWithMerkleHash, // Renamed slightly for C# conventions

        /// <summary>
        /// Initialize using a simple hash.
        /// </summary>
        InitWithSimpleHash // Renamed slightly for C# conventions
    }
} 