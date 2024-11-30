namespace ByteSync.Common.Interfaces.Business;

public interface IEncryptedSessionData
{
    // public string SessionId { get; set; }
    
    public byte[] Data { get; set; }
    
    public byte[] IV { get; set; }
}