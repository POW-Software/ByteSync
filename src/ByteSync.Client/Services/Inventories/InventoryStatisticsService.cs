using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.FileSystems;

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
            .SelectMany(isComplete =>
                isComplete
                    ? Observable
                        .FromAsync(Compute)
                        .Catch<Unit, Exception>(ex =>
                        {
                            _logger.LogError(ex, "InventoryStatisticsService.Compute on AreFullInventoriesComplete");
                            
                            return Observable.Return(Unit.Default);
                        })
                    : Observable.Start(() => { _statisticsSubject.OnNext(null); }))
            .Subscribe();
    }
    
    public IObservable<InventoryStatistics?> Statistics => _statisticsSubject.AsObservable();
    
    public Task Compute()
    {
        return Task.Run(DoCompute);
    }
    
    private void DoCompute()
    {
        var inventoryFiles = _inventoryFileRepository.GetAllInventoriesFiles(LocalInventoryModes.Full);
        
        var statsCollector = new StatisticsCollector();
        
        foreach (var inventoryFile in inventoryFiles)
        {
            ProcessInventoryFile(inventoryFile, statsCollector);
        }
        
        var stats = new InventoryStatistics
        {
            TotalAnalyzed = statsCollector.TotalAnalyzed,
            ProcessedVolume = statsCollector.ProcessedSize,
            AnalyzeSuccess = statsCollector.Success,
            AnalyzeErrors = statsCollector.Errors,
            IdentificationErrors = statsCollector.IdentificationErrors
        };
        
        _statisticsSubject.OnNext(stats);
    }
    
    private void ProcessInventoryFile(InventoryFile inventoryFile, StatisticsCollector collector)
    {
        try
        {
            using var loader = new InventoryLoader(inventoryFile.FullName);
            var inventory = loader.Inventory;
            
            foreach (var part in inventory.InventoryParts)
            {
                foreach (var dir in part.DirectoryDescriptions)
                {
                    if (!dir.IsAccessible)
                    {
                        collector.IdentificationErrors += 1;
                    }
                }
                
                foreach (var fd in part.FileDescriptions)
                {
                    if (!fd.IsAccessible)
                    {
                        collector.IdentificationErrors += 1;
                        continue;
                    }
                    
                    ProcessFileDescription(fd, collector);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inventory file {InventoryFile}", inventoryFile.FullName);
        }
    }
    
    private static void ProcessFileDescription(FileDescription fd, StatisticsCollector collector)
    {
        var hasError = fd.HasAnalysisError;
        var hasFingerprint = HasValidFingerprint(fd);
        
        if (hasError)
        {
            collector.Errors += 1;
        }
        else if (hasFingerprint)
        {
            collector.Success += 1;
        }
        
        if (hasError || hasFingerprint)
        {
            collector.TotalAnalyzed += 1;
            collector.ProcessedSize += fd.Size;
        }
    }
    
    private static bool HasValidFingerprint(FileDescription fd)
    {
        return !string.IsNullOrEmpty(fd.Sha256) || !string.IsNullOrEmpty(fd.SignatureGuid);
    }
    
    private class StatisticsCollector
    {
        public int TotalAnalyzed { get; set; }
        
        public int Success { get; set; }
        
        public int Errors { get; set; }
        
        public int IdentificationErrors { get; set; }
        
        public long ProcessedSize { get; set; }
    }
}
