using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryMainStatusViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ISessionService _sessionService;
    private readonly ITimeTrackingCache _timeTrackingCache;
    private readonly IDialogService _dialogService;
    private readonly ILogger<InventoryMainStatusViewModel> _logger;

    public InventoryMainStatusViewModel()
    {
        
    } 
    
    public InventoryMainStatusViewModel(IInventoryService inventoryService, ISessionService sessionService, 
        ITimeTrackingCache timeTrackingCache, IDialogService dialogService, ILogger<InventoryMainStatusViewModel> logger)
    {
        _inventoryService = inventoryService;
        _sessionService = sessionService;
        _timeTrackingCache = timeTrackingCache;
        _dialogService = dialogService;
        _logger = logger;
        
        AbortIventoryCommand = ReactiveCommand.CreateFromTask(AbortInventory);
        
        EstimatedProcessEndName = Resources.InventoryProcess_EstimatedEnd;
        
        this.WhenActivated(HandleActivation);
    }
    
    private void HandleActivation(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.MainStatus
            .ToPropertyEx(this, x => x.MainStatus)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.MainStatus
            .Select(ms => ms == LocalInventoryPartStatus.Running)
            .ToPropertyEx(this, x => x.IsInventoryRunning)
            .DisposeWith(disposables);

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
    }
    
    public ReactiveCommand<Unit, Unit> AbortIventoryCommand { get; set; }
    
    public extern LocalInventoryPartStatus MainStatus { [ObservableAsProperty] get; }
    
    public extern bool IsInventoryRunning { [ObservableAsProperty] get; }
    
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
    
    private async Task AbortInventory()
    {
        var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
            nameof(Resources.InventoryProcess_AbortInventory_Title), nameof(Resources.InventoryProcess_AbortInventory_Message));
        messageBoxViewModel.ShowYesNo = true;
        var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

        if (result == MessageBoxResult.Yes)
        {
            _logger.LogInformation("inventory aborted on user request");
            
            await _inventoryService.AbortInventory();
        }
    }
}