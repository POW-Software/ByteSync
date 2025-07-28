using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.ServerCommon.Entities.Inventories;

public class InventoryMemberEntity
{
    public InventoryMemberEntity()
    {
        DataNodes = new List<InventoryDataNodeEntity>();
    }
    
    public string SessionId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;

    public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
        
    public DateTimeOffset? LastLocalInventoryStatusUpdate { get; set; }
    
    public List<InventoryDataNodeEntity> DataNodes { get; set; }
}