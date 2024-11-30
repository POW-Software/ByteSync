using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IInventoryRepository : IRepository<InventoryData>
{
    Task<InventoryMemberData?> GetInventoryMember(string sessionId, string clientInstanceId);
}