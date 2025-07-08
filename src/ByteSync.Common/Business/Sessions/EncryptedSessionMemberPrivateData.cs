using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Common.Business.Sessions;

public class EncryptedSessionMemberPrivateData : IEncryptedSessionData
{
    public string Id { get; set; } = null!;

    public byte[] Data { get; set; }
    
    public byte[] IV { get; set; }
}