namespace ByteSync.Common.Interfaces.Business;

public interface IEncryptedSessionData
{
    /// <summary>
    /// Unique identifier of the encrypted payload.
    /// </summary>
    public string Id { get; set; }

    // public string SessionId { get; set; }
    
    public byte[] Data { get; set; }
    
    public byte[] IV { get; set; }
}