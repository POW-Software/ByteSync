using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class InventoryRepository : BaseRepository<InventoryData>, IInventoryRepository
{
    public InventoryRepository(ICacheService cacheService) : base(cacheService)
    {
    }
    
    public override string ElementName { get; } = "Inventory";
    
    public async Task<InventoryMemberData?> GetInventoryMember(string sessionId, string clientInstanceId)
    {
        var inventory = await Get(sessionId);
        
        return inventory?.InventoryMembers.SingleOrDefault(m => m.ClientInstanceId == clientInstanceId);
    }
}