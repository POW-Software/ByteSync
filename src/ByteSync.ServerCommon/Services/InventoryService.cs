using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task ResetSession(string sessionId)
    {
        await _inventoryRepository.UpdateIfExists(sessionId, inventoryData =>
        {
            inventoryData.IsInventoryStarted = false;

            return true;
        });
    }
}