using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class InventoryRepository : BaseRepository<InventoryData>, IInventoryRepository
{
    public InventoryRepository(IRedisInfrastructureService redisInfrastructureService,
        ICacheRepository<InventoryData> cacheRepository) : base(redisInfrastructureService, cacheRepository)
    {
    }
    
    public override EntityType EntityType { get; } = EntityType.Inventory;
    
    public async Task<InventoryMemberData?> GetInventoryMember(string sessionId, string clientInstanceId)
    {
        var inventory = await Get(sessionId);
        
        return inventory?.InventoryMembers.SingleOrDefault(m => m.ClientInstanceId == clientInstanceId);
    }
}