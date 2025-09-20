using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryProcessViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IDialogService _dialogService;
    
    public InventoryProcessViewModel()
    {
    }
    
    public InventoryProcessViewModel(InventoryMainStatusViewModel inventoryMainStatusViewModel,
        InventoryIdentificationViewModel inventoryIdentificationViewModel, InventoryAnalysisViewModel inventoryAnalysisViewModel,
        InventoryBeforeStartViewModel inventoryBeforeStartViewModel, IInventoryService inventoryService, IDialogService dialogService)
    {
        _inventoryService = inventoryService;
        _dialogService = dialogService;
        
        InventoryMainStatusViewModel = inventoryMainStatusViewModel;
        InventoryIdentificationViewModel = inventoryIdentificationViewModel;
        InventoryAnalysisViewModel = inventoryAnalysisViewModel;
        InventoryBeforeStartViewModel = inventoryBeforeStartViewModel;
        
        AbortIventoryCommand = ReactiveCommand.CreateFromTask(AbortInventory);
        
        InventoryProcessData = _inventoryService.InventoryProcessData;
        
        this.WhenActivated(HandleActivation);
    }
    
    private void HandleActivation(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.MainStatus.DistinctUntilChanged()
            .Select(status => status is not InventoryTaskStatus.Pending)
            .ToPropertyEx(this, x => x.HasLocalInventoryStarted)
            .DisposeWith(disposables);
    }
    
    private IObservable<(InventoryMonitorData, InventoryTaskStatus)> SampledMonitorData
    {
        get
        {
            var source = InventoryProcessData.InventoryMonitorObservable.CombineLatest(InventoryProcessData.IdentificationStatus);
            
            Func<(InventoryMonitorData, InventoryTaskStatus), bool> canSkip =
                tuple =>
                {
                    var inventoryMonitorData = tuple.Item1;
                    var localInventoryPartStatus = tuple.Item2;
                    
                    return inventoryMonitorData.HasNonZeroProperty() &&
                           localInventoryPartStatus.In(InventoryTaskStatus.Running);
                };
            
            // Share the source so that it's not subscribed multiple times
            var sharedSource = source.Publish().RefCount();
            
            // Sample the source observable every 0.52 seconds, but only for values that can be skipped
            var sampled = sharedSource
                .Where(canSkip)
                .Sample(TimeSpan.FromSeconds(0.5));
            
            // Get the values from the shared source that can not be skipped
            var notSkipped = sharedSource
                .Where(value => !canSkip(value));
            
            // Merge the sampled and notSkipped sequences
            var merged = sampled.Merge(notSkipped);
            
            return merged;
        }
    }
    
    public extern bool HasLocalInventoryStarted { [ObservableAsProperty] get; }
    
    [Reactive]
    public InventoryMonitorData MonitorData { get; set; }
    
    [Reactive]
    public InventoryProcessData InventoryProcessData { get; set; }
    
    public ReactiveCommand<Unit, Unit> AbortIventoryCommand { get; set; }
    
    public InventoryMainStatusViewModel InventoryMainStatusViewModel { get; set; }
    
    public InventoryIdentificationViewModel InventoryIdentificationViewModel { get; set; }
    
    public InventoryAnalysisViewModel InventoryAnalysisViewModel { get; set; }
    
    public InventoryBeforeStartViewModel InventoryBeforeStartViewModel { get; set; }
    
    private async Task AbortInventory()
    {
        var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
            nameof(Resources.InventoryProcess_AbortInventory_Title), nameof(Resources.InventoryProcess_AbortInventory_Message));
        messageBoxViewModel.ShowYesNo = true;
        var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
        
        if (result == MessageBoxResult.Yes)
        {
            Log.Information("inventory aborted on user request");
            
            _inventoryService.InventoryProcessData?.RequestInventoryAbort();
            
            await _inventoryService.AbortInventory();
        }
    }
}