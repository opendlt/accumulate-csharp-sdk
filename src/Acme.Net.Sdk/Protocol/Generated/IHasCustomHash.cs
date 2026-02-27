namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// For bodies that want to override SHA256(MarshalBinary()) with a custom preimage.
    /// Mirrors Go’s interface { GetHash() []byte }.
    /// </summary>
    public interface IHasCustomHash
    {
        byte[] GetHash();
    }
}
