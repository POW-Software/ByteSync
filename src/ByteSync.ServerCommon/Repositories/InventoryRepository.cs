using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class InventoryRepository : BaseRepository<InventoryEntity>, IInventoryRepository
{
    public InventoryRepository(IRedisInfrastructureService redisInfrastructureService,
        ICacheRepository<InventoryEntity> cacheRepository) : base(redisInfrastructureService, cacheRepository)
    {
    }
    
    public override EntityType EntityType { get; } = EntityType.Inventory;
    
    public async Task<InventoryMemberEntity?> GetInventoryMember(string sessionId, string clientInstanceId)
    {
        var inventory = await Get(sessionId);
        
        return inventory?.InventoryMembers.SingleOrDefault(m => m.ClientInstanceId == clientInstanceId);
    }
}