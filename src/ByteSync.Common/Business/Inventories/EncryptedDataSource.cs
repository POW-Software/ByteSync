using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Common.Business.Inventories;

public class EncryptedDataSource : IEncryptedSessionData
{
    public string Id { get; set; } = null!;

    public byte[] Data { get; set; }
    
    public byte[] IV { get; set; }
    
    public string Code
    {
        get;
        set;
    }
}