using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryMainViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService;
    
    public InventoryMainViewModel()
    {
    }
    
    public InventoryMainViewModel(InventoryMainStatusViewModel inventoryMainStatusViewModel,
        InventoryIdentificationViewModel inventoryIdentificationViewModel,
        InventoryDeltaGenerationViewModel inventoryDeltaGenerationViewModel,
        InventoryBeforeStartViewModel inventoryBeforeStartViewModel,
        IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        
        InventoryMainStatusViewModel = inventoryMainStatusViewModel;
        InventoryIdentificationViewModel = inventoryIdentificationViewModel;
        InventoryDeltaGenerationViewModel = inventoryDeltaGenerationViewModel;
        InventoryBeforeStartViewModel = inventoryBeforeStartViewModel;
        
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
    
    public InventoryMainStatusViewModel InventoryMainStatusViewModel { get; set; }
    
    public InventoryIdentificationViewModel InventoryIdentificationViewModel { get; set; }
    
    public InventoryDeltaGenerationViewModel InventoryDeltaGenerationViewModel { get; set; }
    
    public InventoryBeforeStartViewModel InventoryBeforeStartViewModel { get; set; }
}