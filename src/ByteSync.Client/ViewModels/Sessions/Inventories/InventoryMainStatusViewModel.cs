using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryMainStatusViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService = null!;
    private readonly ISessionService _sessionService = null!;
    private readonly ITimeTrackingCache _timeTrackingCache = null!;
    private readonly IDialogService _dialogService = null!;
    private readonly ILogger<InventoryMainStatusViewModel> _logger = null!;
    
    public InventoryMainStatusViewModel()
    {
    }
    
    public InventoryMainStatusViewModel(IInventoryService inventoryService, ISessionService sessionService,
        ITimeTrackingCache timeTrackingCache, IDialogService dialogService, ILogger<InventoryMainStatusViewModel> logger,
        IInventoryStatisticsService inventoryStatisticsService)
    {
        _inventoryService = inventoryService;
        _sessionService = sessionService;
        _timeTrackingCache = timeTrackingCache;
        _dialogService = dialogService;
        _logger = logger;
        
        AbortIventoryCommand = ReactiveCommand.CreateFromTask(AbortInventory);
        
        EstimatedProcessEndName = Resources.InventoryProcess_EstimatedEnd;
        
        this.WhenActivated(disposables =>
        {
            HandleActivation(disposables);
            
            _inventoryService.InventoryProcessData.AnalysisStatus
                .ToPropertyEx(this, x => x.AnalysisStatus)
                .DisposeWith(disposables);
            
            _inventoryService.InventoryProcessData.AnalysisStatus
                .Select(ms => ms == InventoryTaskStatus.Success)
                .ToPropertyEx(this, x => x.ShowGlobalStatistics)
                .DisposeWith(disposables);
            
            inventoryStatisticsService.Statistics
                .Subscribe(s =>
                {
                    GlobalTotalAnalyzed = s?.TotalAnalyzed;
                    GlobalAnalyzeSuccess = s?.Success;
                    GlobalAnalyzeErrors = s?.Errors;
                })
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.GlobalAnalyzeErrors)
                .Select(e => (e ?? 0) > 0)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.HasErrors)
                .DisposeWith(disposables);
        });
    }
    
    private void HandleActivation(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.GlobalMainStatus
            .ToPropertyEx(this, x => x.GlobalMainStatus)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.GlobalMainStatus
            .Select(ms => ms == InventoryTaskStatus.Running)
            .ToPropertyEx(this, x => x.IsInventoryRunning)
            .DisposeWith(disposables);
        
        // Visible while global inventory is not in a terminal state
        _inventoryService.InventoryProcessData.GlobalMainStatus
            .Select(ms => ms is InventoryTaskStatus.Pending or InventoryTaskStatus.Running)
            .ToPropertyEx(this, x => x.IsInventoryInProgress)
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
    
    public ReactiveCommand<Unit, Unit> AbortIventoryCommand { get; set; } = null!;
    
    public extern InventoryTaskStatus GlobalMainStatus { [ObservableAsProperty] get; }
    
    public extern bool IsInventoryRunning { [ObservableAsProperty] get; }
    
    public extern InventoryTaskStatus AnalysisStatus { [ObservableAsProperty] get; }
    
    public extern bool IsInventoryInProgress { [ObservableAsProperty] get; }
    
    public extern bool ShowGlobalStatistics { [ObservableAsProperty] get; }
    
    [Reactive]
    public string EstimatedProcessEndName { get; set; } = null!;
    
    [Reactive]
    public DateTime? StartDateTime { get; set; }
    
    [Reactive]
    public TimeSpan ElapsedTime { get; set; }
    
    [Reactive]
    public DateTime? EstimatedEndDateTime { get; set; }
    
    [Reactive]
    public TimeSpan? RemainingTime { get; set; }
    
    [Reactive]
    public int? GlobalTotalAnalyzed { get; set; }
    
    [Reactive]
    public int? GlobalAnalyzeSuccess { get; set; }
    
    [Reactive]
    public int? GlobalAnalyzeErrors { get; set; }
    
    public extern bool HasErrors { [ObservableAsProperty] get; }
    
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