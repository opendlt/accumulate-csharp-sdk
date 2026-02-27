namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Defines the vote types for multi-signature transactions.
    /// Matches the Go core VoteType enum values.
    /// </summary>
    public enum VoteType
    {
        Accept = 0,
        Reject = 1,
        Abstain = 2,
        Suggest = 3
    }
}
