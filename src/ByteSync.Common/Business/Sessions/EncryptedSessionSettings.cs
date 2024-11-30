using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Common.Business.Sessions;

public class EncryptedSessionSettings : IEncryptedSessionData
{
    public byte[] Data { get; set; }
    
    public byte[] IV { get; set; }
}