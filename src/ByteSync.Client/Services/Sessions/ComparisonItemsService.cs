using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using DynamicData;

namespace ByteSync.Services.Sessions;

public class ComparisonItemsService : IComparisonItemsService
{
    private readonly ISessionService _sessionService;
    private readonly IInventoryService _inventoryService;
    private readonly IDataPartIndexer _dataPartIndexer;
    private readonly IInventoryFileRepository _inventoryFileRepository;

    public ComparisonItemsService(ISessionService sessionService, IInventoryService inventoriesService, IInventoryFileRepository inventoryFileRepository, 
        IDataPartIndexer dataPartIndexer)
    {
        _sessionService = sessionService;
        _inventoryService = inventoriesService;
        _inventoryFileRepository = inventoryFileRepository;
        _dataPartIndexer = dataPartIndexer;
        
        ComparisonItemsCache = new SourceCache<ComparisonItem, PathIdentity>(comparisonItem => comparisonItem.PathIdentity);
        
        ComparisonItems = ComparisonItemsCache.AsObservableCache();
        
        ComparisonResult = new ReplaySubject<ComparisonResult?>(1);
        ComparisonResult.OnNext(null);
        
        _inventoryService.InventoryProcessData.AreFullInventoriesComplete
            .DistinctUntilChanged()
            .Where(b => b is true)
            .Subscribe(_ =>
            {
                ComputeComparisonResult().ConfigureAwait(false).GetAwaiter().GetResult();
            });

        ComparisonResult
            .DistinctUntilChanged()
            .Where(c => c != null)
            .Subscribe(comparisonResult =>
            {
                ComparisonItemsCache.AddOrUpdate(comparisonResult!.ComparisonItems);
                ApplySynchronizationRules();
            });
        
        _sessionService.SessionObservable
            .Where(x => x == null)
            .Subscribe(_ =>
            {
                ComparisonItemsCache.Clear();
            });
        
        _sessionService.SessionStatusObservable
            .Where(x => x == SessionStatus.Preparation)
            .Subscribe(_ =>
            {
                ComparisonItemsCache.Clear();
            });
    }

    public SourceCache<ComparisonItem, PathIdentity> ComparisonItemsCache { get; set; }

    public IObservableCache<ComparisonItem, PathIdentity> ComparisonItems { get; }
    
    public ISubject<ComparisonResult?> ComparisonResult { get; set; }


    private async Task ComputeComparisonResult()
    {
        using var inventoryComparer = new InventoryComparer(_sessionService.CurrentSessionSettings!);
                
        var inventoriesFiles = _inventoryFileRepository.GetAllInventoriesFiles(LocalInventoryModes.Full);
        inventoryComparer.AddInventories(inventoriesFiles);
        var comparisonResult = inventoryComparer.Compare();

        _dataPartIndexer.BuildMap(comparisonResult.Inventories);
        
        ComparisonResult.OnNext(comparisonResult);

        await _sessionService.SetSessionStatus(SessionStatus.Comparison);
    }
    
    public Task ApplySynchronizationRules()
    {
        return Task.CompletedTask;
    }
}