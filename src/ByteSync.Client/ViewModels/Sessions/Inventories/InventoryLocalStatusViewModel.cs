using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryLocalStatusViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService = null!;
    private readonly ITimeTrackingCache _timeTrackingCache = null!;
    private readonly IInventoryService _inventoryService = null!;
    
    public InventoryLocalStatusViewModel()
    {
        EstimatedProcessEndName = Resources.InventoryProcess_EstimatedEnd;
    }
    
    public InventoryLocalStatusViewModel(ISessionService sessionService, ITimeTrackingCache timeTrackingCache,
        IInventoryService inventoryService) : this()
    {
        _sessionService = sessionService;
        _timeTrackingCache = timeTrackingCache;
        _inventoryService = inventoryService;
        
        this.WhenActivated(disposables =>
        {
            var timeTrackingComputer = _timeTrackingCache
                .GetTimeTrackingComputer(_sessionService.SessionId!, TimeTrackingComputerType.Inventory)
                .Result;
            timeTrackingComputer.RemainingTime
                .Subscribe(remainingTime =>
                {
                    RemainingTime = remainingTime.RemainingTime;
                    ElapsedTime = remainingTime.ElapsedTime;
                    EstimatedEndDateTime = remainingTime.EstimatedEndDateTime;
                    StartDateTime = remainingTime.StartDateTime;
                })
                .DisposeWith(disposables);
            
            _inventoryService.InventoryProcessData.GlobalMainStatus
                .Select(s => s is InventoryTaskStatus.Pending or InventoryTaskStatus.Running
                    ? Resources.InventoryProcess_EstimatedEnd
                    : Resources.InventoryProcess_End)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(text => EstimatedProcessEndName = text)
                .DisposeWith(disposables);
        });
    }
    
    [Reactive]
    public string EstimatedProcessEndName { get; set; }
    
    [Reactive]
    public DateTime? StartDateTime { get; set; }
    
    [Reactive]
    public TimeSpan ElapsedTime { get; set; }
    
    [Reactive]
    public DateTime? EstimatedEndDateTime { get; set; }
    
    [Reactive]
    public TimeSpan? RemainingTime { get; set; }
}