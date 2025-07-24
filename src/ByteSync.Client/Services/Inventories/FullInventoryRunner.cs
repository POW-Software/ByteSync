using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class FullInventoryRunner : IFullInventoryRunner
{
    private readonly IInventoryFinishedService _inventoryFinishedService;
    private readonly IInventoryService _inventoryService;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly IInventoryComparerFactory _inventoryComparerFactory;
    private readonly ILogger<FullInventoryRunner> _logger;

    public FullInventoryRunner(IInventoryFinishedService inventoryFinishedService, IInventoryService inventoryService, 
        ICloudSessionLocalDataManager cloudSessionLocalDataManager, IInventoryComparerFactory inventoryComparerFactory, ILogger<FullInventoryRunner> logger)
    {
        _inventoryFinishedService = inventoryFinishedService;
        _inventoryService = inventoryService;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _inventoryComparerFactory = inventoryComparerFactory;
        _logger = logger;
    }
    
    private InventoryProcessData InventoryProcessData
    {
        get
        {
            return _inventoryService.InventoryProcessData;
        }
    }
    
    public async Task<bool> RunFullInventory()
    {
        bool isOK;
        
        try
        {
            InventoryProcessData.AnalysisStatus.OnNext(LocalInventoryPartStatus.Running);

            var inventoriesBuildersAndItems = new List<Tuple<IInventoryBuilder, HashSet<IndexedItem>>>();
            foreach (var inventoryBuilder in InventoryProcessData.InventoryBuilders!)
            {
                using var inventoryComparer = _inventoryComparerFactory.CreateInventoryComparer(LocalInventoryModes.Base, inventoryBuilder.Indexer);
                var comparisonResult = inventoryComparer.Compare();
                
                var filesIdentifier = new FilesIdentifier(inventoryBuilder.Inventory, inventoryBuilder.SessionSettings!, inventoryBuilder.Indexer);
                HashSet<IndexedItem> items = filesIdentifier.Identify(comparisonResult);
                InventoryProcessData.UpdateMonitorData(monitorData => monitorData.AnalyzableFiles += items.Count);
                
                inventoriesBuildersAndItems.Add(new (inventoryBuilder, items));
            }

            await Parallel.ForEachAsync(inventoriesBuildersAndItems, 
                new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = InventoryProcessData.CancellationTokenSource.Token}, 
                async (tuple, token) =>
                {
                    var inventoryBuilder = tuple.Item1;
                    var items = tuple.Item2;

                    var fullInventoryFullName = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventoryBuilder.InventoryCode, LocalInventoryModes.Full);
                    await inventoryBuilder.RunAnalysisAsync(fullInventoryFullName, items, token);
                });
            
            if (!InventoryProcessData.CancellationTokenSource.Token.IsCancellationRequested)
            {
                InventoryProcessData.AnalysisStatus.OnNext(LocalInventoryPartStatus.Success);
                InventoryProcessData.MainStatus.OnNext(LocalInventoryPartStatus.Success);
                
                await _inventoryFinishedService.SetLocalInventoryFinished(InventoryProcessData.Inventories!, LocalInventoryModes.Full);
            }
            
            isOK = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RunFullInventory");
            isOK = false;
            
            InventoryProcessData.SetError(ex);
        }

        return isOK;
    }
}