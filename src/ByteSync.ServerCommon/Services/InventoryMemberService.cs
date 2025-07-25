using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Services;

public class InventoryMemberService : IInventoryMemberService
{
    public InventoryMemberEntity GetOrCreateInventoryMember(InventoryEntity inventoryEntity, string sessionId, Client client)
    {
        var inventoryMember = inventoryEntity.InventoryMembers.SingleOrDefault(imd => imd.ClientInstanceId == client.ClientInstanceId);

        if (inventoryMember == null)
        {
            inventoryMember = new InventoryMemberEntity
            {
                SessionId = sessionId,
                ClientInstanceId = client.ClientInstanceId,
                SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryWaitingForStart,
            };

            inventoryEntity.InventoryMembers.Add(inventoryMember);
        }

        return inventoryMember;
    }
}