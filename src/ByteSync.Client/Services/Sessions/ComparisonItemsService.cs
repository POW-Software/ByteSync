using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;

namespace ByteSync.Services.Sessions;

public class ComparisonItemsService : IComparisonItemsService
{
    private readonly ISessionService _sessionService;
    private readonly IInventoryService _inventoryService;
    private readonly IDataPartIndexer _dataPartIndexer;
    private readonly IComparisonItemRepository _comparisonItemRepository;
    private readonly IInventoryFileRepository _inventoryFileRepository;

    public ComparisonItemsService(ISessionService sessionService, IInventoryService inventoriesService, IInventoryFileRepository inventoryFileRepository, 
        IComparisonItemRepository comparisonItemRepository, IDataPartIndexer dataPartIndexer)
    {
        _sessionService = sessionService;
        _inventoryService = inventoriesService;
        _inventoryFileRepository = inventoryFileRepository;
        _comparisonItemRepository = comparisonItemRepository;
        _dataPartIndexer = dataPartIndexer;
        
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
                _comparisonItemRepository.AddOrUpdate(comparisonResult!.ComparisonItems);
                ApplySynchronizationRules();
            });
        
        _sessionService.SessionObservable
            .Where(x => x == null)
            .Subscribe(_ =>
            {
                _comparisonItemRepository.Clear();
            });
        
        _sessionService.SessionStatusObservable
            .Where(x => x == SessionStatus.Preparation)
            .Subscribe(_ =>
            {
                _comparisonItemRepository.Clear();
            });
    }
    
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