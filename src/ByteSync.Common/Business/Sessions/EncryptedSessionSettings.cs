using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Common.Business.Sessions;

public class EncryptedSessionSettings : IEncryptedSessionData
{
    public byte[] Data { get; set; } = null!;
    
    public byte[] IV { get; set; } = null!;
}