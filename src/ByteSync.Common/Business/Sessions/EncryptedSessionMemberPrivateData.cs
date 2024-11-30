using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Common.Business.Sessions;

public class EncryptedSessionMemberPrivateData : IEncryptedSessionData
{
    public byte[] Data { get; set; }
    
    public byte[] IV { get; set; }
}