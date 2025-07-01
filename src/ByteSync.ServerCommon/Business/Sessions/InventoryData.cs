namespace ByteSync.ServerCommon.Business.Sessions;

public class InventoryData
{
    public InventoryData()
    {
        InventoryMembers = new List<InventoryMemberData>();
    }

    public InventoryData(string sessionId) : this()
    {
        SessionId = sessionId;
    }

    public string SessionId { get; set; } = null!;
    
    public List<InventoryMemberData> InventoryMembers { get; set; }
    
    public bool IsInventoryStarted { get; set; }
}