using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities.Inventories;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IInventoryMemberService
{
    InventoryMemberEntity GetOrCreateInventoryMember(InventoryEntity inventoryEntity, string sessionId, Client client);
}