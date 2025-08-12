namespace ByteSync.ServerCommon.Entities.Inventories;

public class InventoryEntity
{
    public InventoryEntity()
    {
        InventoryMembers = new List<InventoryMemberEntity>();
    }

    public InventoryEntity(string sessionId) : this()
    {
        SessionId = sessionId;
    }

    public string SessionId { get; set; } = null!;
    
    public List<InventoryMemberEntity> InventoryMembers { get; set; }
    
    public bool IsInventoryStarted { get; set; }
}