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

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryProcessViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<InventoryProcessViewModel> _logger;
    
    public InventoryProcessViewModel()
    {
    }
    
    public InventoryProcessViewModel(InventoryMainStatusViewModel inventoryMainStatusViewModel,
        InventoryIdentificationViewModel inventoryIdentificationViewModel, InventoryAnalysisViewModel inventoryAnalysisViewModel,
        InventoryBeforeStartViewModel inventoryBeforeStartViewModel, IInventoryService inventoryService, IDialogService dialogService,
        ILogger<InventoryProcessViewModel> logger)
    {
        _inventoryService = inventoryService;
        _dialogService = dialogService;
        _logger = logger;
        
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
    
    public extern bool HasLocalInventoryStarted { [ObservableAsProperty] get; }
    
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
            _logger.LogInformation("inventory aborted on user request");
            
            _inventoryService.InventoryProcessData.RequestInventoryAbort();
            
            await _inventoryService.AbortInventory();
        }
    }
}