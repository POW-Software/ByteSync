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

        //////
        /* // todo 040423
        SetComparisonResult(comparisonResult);

        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                SessionFullDetails.IsInventoriesComparisonDone = true;
            }
        }

        await _cloudSessionEventsHub.RaiseInventoriesComparisonDone(comparisonResult);
        */
        //////
    }
    
    /*
    public void SetComparisonResult(ComparisonResult comparisonResult)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                SessionFullDetails.ComparisonResult = comparisonResult;

                SessionFullDetails.DataPartMapper = new DataPartMapper(SessionFullDetails.ComparisonResult?.Inventories);
                
                if (IsSessionCreatedByMe && ! HasSessionBeenRestarted && SessionFullDetails.RunSessionProfileInfo != null)
                {
                    // On reconvertit les SynchronizationRules
                    SynchronizationRulesConverter converter = new SynchronizationRulesConverter();
                    var synchronizationRuleViewModels = converter.ConvertToSynchronizationRuleViewModels(
                        SessionFullDetails.RunSessionProfileInfo.GetProfileDetails().SynchronizationRules,
                        SessionFullDetails.DataPartMapper);
                
                    SessionFullDetails.SynchronizationRules.AddAll(synchronizationRuleViewModels);
                }

                SessionFullDetails.ComparisonResultSet.Set();
            }
        }
    }
    */
    
    /*
    private void InitializeComparisonItems(ComparisonResult comparisonResult)
    {
        throw new NotImplementedException();
    }
    */
    
    /*
    public async Task InitializeComparisonItems()
    {
        var list = await BuildComparisonItemViewModelList();
        
        ComparisonItemsCache.AddOrUpdate(list);
        
        // ComparisonItems.
        // var disposable = ComparisonItems.SuspendNotifications();
        // ComparisonItems.AddRange(list);
        // disposable.Dispose();

        await ApplySynchronizationRules();
    }
    
    
    private async Task<List<ComparisonItemViewModel>> BuildComparisonItemViewModelList()
    {
        return await Task.Run(() =>
        {
            List<ComparisonItemViewModel> list = new List<ComparisonItemViewModel>();
            foreach (var resultComparisonItem in ComparisonResult!.ComparisonItems.OrderBy(c => c.PathIdentity.LinkingKeyValue))
            {
                var comparisonItemView = new ComparisonItemViewModel(resultComparisonItem, ComparisonResult.Inventories);

                list.Add(comparisonItemView);
            }
    
            return list;
        });
    }*/
    
    public Task ApplySynchronizationRules()
    {
        return Task.CompletedTask;
        
        // todo 040423
        /*
        return Task.Run(async () =>
        {
            // Il faut remapper les règles existantes car les inventaires ont pu changer
            var synchronizationRules = SynchronizationRules;
        
            if (synchronizationRules != null)
            {
                DataPartMapper!.Remap(synchronizationRules);

                await UpdateComparisonItemsActions();
        
                await UpdateSharedSynchronizationActions();

                await _cloudSessionEventsHub.RaiseSynchronizationRulesApplied();
            }
        });*/
    }
    
    private async Task UpdateComparisonItemsActions()
    {
        // todo 040423
        /*
        if (HasSynchronizationStarted)
        {
            return;
        }
        
        var allSynchronizationRules = SynchronizationRules!.Select(vm => vm.SynchronizationRule).ToList();
        await _uiHelper.ExecuteOnUi(() =>
        {
            SynchronizationRuleMatcher synchronizationRuleMatcher = new SynchronizationRuleMatcher(this);
            synchronizationRuleMatcher.MakeMatches(ComparisonItems, allSynchronizationRules);
        });
        */
    }
}