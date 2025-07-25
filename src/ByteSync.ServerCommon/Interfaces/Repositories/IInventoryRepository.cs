using ByteSync.ServerCommon.Entities.Inventories;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IInventoryRepository : IRepository<InventoryEntity>
{
    Task<InventoryMemberEntity?> GetInventoryMember(string sessionId, string clientInstanceId);
}