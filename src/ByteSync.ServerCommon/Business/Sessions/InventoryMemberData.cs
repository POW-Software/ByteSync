using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.ServerCommon.Business.Sessions;

public class InventoryMemberData
{
    public InventoryMemberData()
    {
        SharedPathItems = new List<EncryptedPathItem>();
    }
    
    public string SessionId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;

    public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
        
    public DateTimeOffset? LastLocalInventoryStatusUpdate { get; set; }
    
    public List<EncryptedPathItem> SharedPathItems { get; set; }
}