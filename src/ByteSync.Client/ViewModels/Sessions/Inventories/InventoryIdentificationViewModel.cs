using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryIdentificationViewModel : ActivableViewModelBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryIdentificationViewModel()
    {
        
    } 
    
    public InventoryIdentificationViewModel(IInventoryService inventoryService )
    {
        _inventoryService = inventoryService;
        
        this.WhenActivated(HandleActivation);
    }
    
    private void HandleActivation(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.IdentificationStatus
            .ToPropertyEx(this, x => x.IdentificationStatus)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.IdentificationStatus
            .Select(ms => ms == LocalInventoryPartStatus.Running)
            .ToPropertyEx(this, x => x.IsIdentificationRunning)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.InventoryMonitorObservable
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(m =>
            {
                IdentifiedFiles = m.IdentifiedFiles;
                IdentifiedDirectories = m.IdentifiedDirectories;
                IdentifiedSize = m.IdentifiedSize;
            })
            .DisposeWith(disposables);
    }
    
    public extern LocalInventoryPartStatus IdentificationStatus { [ObservableAsProperty] get; }
    
    public extern bool IsIdentificationRunning { [ObservableAsProperty] get; }
    
    [Reactive]
    public int IdentifiedFiles { get; set; }
    
    [Reactive]
    public int IdentifiedDirectories { get; set; }
    
    [Reactive]
    public long IdentifiedSize { get; set; }
    
    // public extern int IdentifiedFiles { [ObservableAsProperty] get; }
    //
    // public extern int IdentifiedDirectories { [ObservableAsProperty] get; }
}