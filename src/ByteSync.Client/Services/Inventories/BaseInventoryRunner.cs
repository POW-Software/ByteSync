using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class BaseInventoryRunner : IBaseInventoryRunner
{
    private readonly IInventoryFinishedService _inventoryFinishedService;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<BaseInventoryRunner> _logger;

    public BaseInventoryRunner(IInventoryFinishedService inventoryFinishedService, ICloudSessionLocalDataManager cloudSessionLocalDataManager, 
        IInventoryService inventoryService, ILogger<BaseInventoryRunner> logger)
    {
        _inventoryFinishedService = inventoryFinishedService;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _inventoryService = inventoryService;
        _logger = logger;
    }
    
    private InventoryProcessData InventoryProcessData
    {
        get
        {
            return _inventoryService.InventoryProcessData;
        }
    }

    public async Task<bool> RunBaseInventory()
    {
        bool isOK;
        try
        {
            await Parallel.ForEachAsync(InventoryProcessData.InventoryBuilders!, 
                new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = InventoryProcessData.CancellationTokenSource.Token}, 
                async (builder, token) =>
                {
                    var baseInventoryFullName = _cloudSessionLocalDataManager
                        .GetCurrentMachineInventoryPath(builder.InventoryCode, LocalInventoryModes.Base);

                    await builder.BuildBaseInventoryAsync(baseInventoryFullName, token);
                });

            if (!InventoryProcessData.CancellationTokenSource.Token.IsCancellationRequested)
            {
                InventoryProcessData.IdentificationStatus.OnNext(LocalInventoryPartStatus.Success);
                
                await _inventoryFinishedService.SetLocalInventoryFinished(InventoryProcessData.Inventories!, LocalInventoryModes.Base);
            }
            
            isOK = true;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "BaseInventoryRunner");
            isOK = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BaseInventoryRunner");
            isOK = false;

            InventoryProcessData.SetError(ex);
        }

        return isOK;
    }
}