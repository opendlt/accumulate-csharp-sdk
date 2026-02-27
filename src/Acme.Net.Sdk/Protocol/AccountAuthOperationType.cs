namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Defines the operation types for UpdateAccountAuth transactions.
    /// Matches the Go core AccountAuthOperationType enum values.
    /// </summary>
    public enum AccountAuthOperationType
    {
        Enable = 1,
        Disable = 2,
        AddAuthority = 3,
        RemoveAuthority = 4
    }
}
