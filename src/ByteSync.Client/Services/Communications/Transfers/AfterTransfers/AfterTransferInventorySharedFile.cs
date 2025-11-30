using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Inventories;

namespace ByteSync.Services.Communications.Transfers.AfterTransfers;

public class AfterTransferInventorySharedFile : IAfterTransferSharedFile
{
    private readonly IInventoryService _inventoryService;
    
    public AfterTransferInventorySharedFile(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }
    
    public Task OnFilePartUploaded(SharedFileDefinition sharedFileDefinition)
    {
        return Task.CompletedTask;
    }
    
    public Task OnUploadFinished(SharedFileDefinition sharedFileDefinition)
    {
        return Task.CompletedTask;
    }
    
    public Task OnFilePartUploadedError(SharedFileDefinition sharedFileDefinition, Exception exception)
    {
        _inventoryService.InventoryProcessData.InventoryTransferError.OnNext(true);
        
        return Task.CompletedTask;
    }
    
    public Task OnUploadFinishedError(SharedFileDefinition sharedFileDefinition, Exception exception)
    {
        _inventoryService.InventoryProcessData.InventoryTransferError.OnNext(true);
        
        return Task.CompletedTask;
    }
}