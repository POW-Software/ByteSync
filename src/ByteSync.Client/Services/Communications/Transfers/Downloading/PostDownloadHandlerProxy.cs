using ByteSync.Business.Communications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Profiles;

namespace ByteSync.Services.Communications.Transfers.Downloading;

public class PostDownloadHandlerProxy : IPostDownloadHandlerProxy
{
    private readonly ISessionProfileManager _sessionProfileManager;
    private readonly ISynchronizationDataReceiver _synchronizationDataReceiver;
    private readonly IInventoryService _inventoryService;
    
    public PostDownloadHandlerProxy(ISessionProfileManager sessionProfileManager, IInventoryService inventoriesService, 
        ISynchronizationDataReceiver synchronizationDataReceiver)
    {
        _sessionProfileManager = sessionProfileManager;
        _inventoryService = inventoriesService;
        _synchronizationDataReceiver = synchronizationDataReceiver;
    }

    public async Task HandleDownloadFinished(LocalSharedFile? localSharedFile)
    {
        if (localSharedFile != null)
        {
            if (localSharedFile.SharedFileDefinition.IsProfileDetails)
            {
                await _sessionProfileManager.OnFileIsFullyDownloaded(localSharedFile);
            }
            
            else if (localSharedFile.SharedFileDefinition.IsSynchronizationStartData)
            {
                await _synchronizationDataReceiver.OnSynchronizationDataFileDownloaded(localSharedFile);
            }
            else if (localSharedFile.SharedFileDefinition.IsInventory)
            {
                await _inventoryService.OnFileIsFullyDownloaded(localSharedFile);
            }
        }
    }
}