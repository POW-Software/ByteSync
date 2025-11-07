using System.IO;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Inventories;
using ByteSync.Services.Misc;

namespace ByteSync.Services.Inventories;

public class InventoryFinishedService : IInventoryFinishedService
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly IFileUploaderFactory _fileUploaderFactory;
    private readonly IInventoryService _inventoryService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly ILogger<InventoryFinishedService> _logger;
    
    public InventoryFinishedService(ISessionService sessionService, ICloudSessionLocalDataManager cloudSessionLocalDataManager,
        IFileUploaderFactory fileUploaderFactory, IInventoryService inventoryService, ISessionMemberService sessionMemberService,
        ILogger<InventoryFinishedService> logger)
    {
        _sessionService = sessionService;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _fileUploaderFactory = fileUploaderFactory;
        _inventoryService = inventoryService;
        _sessionMemberService = sessionMemberService;
        _logger = logger;
    }
    
    public async Task SetLocalInventoryFinished(List<Inventory> inventories, LocalInventoryModes localInventoryMode)
    {
        var inventoriesFiles = BuildInventoriesLocalSharedFiles(inventories, localInventoryMode);
        
        if (_sessionService.CurrentSession is CloudSession)
        {
            try
            {
                long totalBytes = 0;
                foreach (var localSharedFile in inventoriesFiles)
                {
                    var fi = new FileInfo(localSharedFile.FullName);
                    if (fi.Exists)
                    {
                        totalBytes += fi.Length;
                    }
                }
                
                _inventoryService.InventoryProcessData.UpdateMonitorData(m => { m.UploadTotalVolume += totalBytes; });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to calculate total upload volume for inventory files. Upload will continue but progress tracking may be incorrect");
            }
            
            foreach (var localSharedFile in inventoriesFiles)
            {
                var fileUploader = _fileUploaderFactory.Build(localSharedFile.FullName, localSharedFile.SharedFileDefinition);
                await fileUploader.Upload();
            }
        }
        
        await _inventoryService.SetLocalInventory(inventoriesFiles, localInventoryMode);
        
        await _sessionMemberService.UpdateCurrentMemberGeneralStatus(localInventoryMode.ConvertFinishInventory());
    }
    
    private List<InventoryFile> BuildInventoriesLocalSharedFiles(List<Inventory> inventories, LocalInventoryModes localInventoryMode)
    {
        var session = _sessionService.CurrentSession!;
        
        List<InventoryFile> result = new List<InventoryFile>();
        foreach (var inventory in inventories)
        {
            var inventoryFullName = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventory, localInventoryMode);
            
            var sharedFileDefinition = new SharedFileDefinition();
            
            if (localInventoryMode == LocalInventoryModes.Base)
            {
                sharedFileDefinition.SharedFileType = SharedFileTypes.BaseInventory;
            }
            else
            {
                sharedFileDefinition.SharedFileType = SharedFileTypes.FullInventory;
            }
            
            sharedFileDefinition.ClientInstanceId = inventory.Endpoint.ClientInstanceId;
            sharedFileDefinition.SessionId = session.SessionId;
            sharedFileDefinition.AdditionalName = inventory.CodeAndId;
            
            var inventoryFile = new InventoryFile(sharedFileDefinition, inventoryFullName);
            
            result.Add(inventoryFile);
        }
        
        return result;
    }
}