using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Common.Business.Sessions;

public class EncryptedDataNode : IEncryptedSessionData
{
    public string Id { get; set; } = null!;

    public byte[] Data { get; set; } = null!;

    public byte[] IV { get; set; } = null!;
}
