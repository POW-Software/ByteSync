using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Inventories;

public class InventoryStatisticsService : IInventoryStatisticsService
{
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryFileRepository _inventoryFileRepository;
    private readonly ILogger<InventoryStatisticsService> _logger;
    
    private readonly BehaviorSubject<InventoryStatistics?> _statisticsSubject;
    
    public InventoryStatisticsService(IInventoryService inventoryService, IInventoryFileRepository inventoryFileRepository,
        ILogger<InventoryStatisticsService> logger)
    {
        _inventoryService = inventoryService;
        _inventoryFileRepository = inventoryFileRepository;
        _logger = logger;
        
        _statisticsSubject = new BehaviorSubject<InventoryStatistics?>(null);
        
        _inventoryService.InventoryProcessData.AreFullInventoriesComplete
            .DistinctUntilChanged()
            .Subscribe(async isComplete =>
            {
                if (isComplete)
                {
                    try
                    {
                        await Compute();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "InventoryStatisticsService.Compute on AreFullInventoriesComplete");
                    }
                }
                else
                {
                    _statisticsSubject.OnNext(null);
                }
            });
    }
    
    public IObservable<InventoryStatistics?> Statistics => _statisticsSubject.AsObservable();
    
    public Task Compute()
    {
        return Task.Run(DoCompute);
    }
    
    private void DoCompute()
    {
        var inventoryFiles = _inventoryFileRepository.GetAllInventoriesFiles(LocalInventoryModes.Full);
        
        var totalAnalyzed = 0;
        var success = 0;
        var errors = 0;
        long processedSize = 0;
        
        foreach (var inventoryFile in inventoryFiles)
        {
            try
            {
                using var loader = new InventoryLoader(inventoryFile.FullName);
                var inventory = loader.Inventory;
                
                foreach (var part in inventory.InventoryParts)
                {
                    foreach (var fd in part.FileDescriptions)
                    {
                        var hasError = fd.HasAnalysisError;
                        var hasFingerprint = !string.IsNullOrEmpty(fd.Sha256) || !string.IsNullOrEmpty(fd.SignatureGuid);
                        
                        if (hasError)
                        {
                            errors += 1;
                        }
                        else if (hasFingerprint)
                        {
                            success += 1;
                        }
                        
                        if (hasError || hasFingerprint)
                        {
                            totalAnalyzed += 1;
                            processedSize += fd.Size;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load inventory file {InventoryFile}", inventoryFile.FullName);
            }
        }
        
        var stats = new InventoryStatistics
        {
            TotalAnalyzed = totalAnalyzed,
            ProcessedSize = processedSize,
            Success = success,
            Errors = errors
        };
        
        _statisticsSubject.OnNext(stats);
    }
}