using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
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
    private readonly IThemeService _themeService = null!;
    
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
            
            // Use shared streams and combine for clean gating
            var statusStream = _inventoryService.InventoryProcessData.GlobalMainStatus
                .DistinctUntilChanged()
                .Publish()
                .RefCount();
            var statsStream = inventoryStatisticsService.Statistics
                .Publish()
                .RefCount();
            
            // Immediate render for non-success terminal statuses
            statusStream
                .Where(st => st is InventoryTaskStatus.Error or InventoryTaskStatus.Cancelled or InventoryTaskStatus.NotLaunched)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(st =>
                {
                    GlobalMainIcon = "SolidXCircle";
                    var key = $"InventoryProcess_Inventory{st}";
                    GlobalMainStatusText = Resources.ResourceManager.GetString(key, Resources.Culture) ?? string.Empty;
                    GlobalMainIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                    IsWaitingFinalStats = false;
                })
                .DisposeWith(disposables);
            
            // On Success, wait (via CombineLatest) until statistics are available, then render once
            statusStream
                .Where(st => st == InventoryTaskStatus.Success)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => IsWaitingFinalStats = true)
                .DisposeWith(disposables);
            
            statusStream
                .Where(st => st == InventoryTaskStatus.Success)
                .CombineLatest(statsStream, (_, s) => s)
                .Take(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s =>
                {
                    var errors = s?.Errors ?? 0;
                    if (errors > 0)
                    {
                        GlobalMainIcon = "RegularError";
                        GlobalMainStatusText =
                            Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                            ?? Resources.InventoryProcess_InventorySuccess;
                        GlobalMainIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                    }
                    else
                    {
                        GlobalMainIcon = "SolidCheckCircle";
                        GlobalMainStatusText = Resources.InventoryProcess_InventorySuccess;
                        GlobalMainIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                    }
                    
                    IsWaitingFinalStats = false;
                })
                .DisposeWith(disposables);
        });
    }
    
    // // Overload with theme service for color selection
    // public InventoryMainStatusViewModel(IInventoryService inventoryService, ISessionService sessionService,
    //     ITimeTrackingCache timeTrackingCache, IDialogService dialogService, ILogger<InventoryMainStatusViewModel> logger,
    //     IInventoryStatisticsService inventoryStatisticsService, IThemeService themeService)
    //     : this(inventoryService, sessionService, timeTrackingCache, dialogService, logger, inventoryStatisticsService)
    // {
    //     _themeService = themeService;
    //     GlobalMainIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
    // }
    
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
        var runningOrPending = _inventoryService.InventoryProcessData.GlobalMainStatus
            .Select(ms => ms is InventoryTaskStatus.Pending or InventoryTaskStatus.Running);
        runningOrPending
            .CombineLatest(this.WhenAnyValue(x => x.IsWaitingFinalStats).StartWith(false), (rp, wait) => rp || wait)
            .ObserveOn(RxApp.MainThreadScheduler)
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
    
    [Reactive]
    public string GlobalMainIcon { get; set; } = "None";
    
    [Reactive]
    public string GlobalMainStatusText { get; set; } = string.Empty;
    
    [Reactive]
    public IBrush? GlobalMainIconBrush { get; set; }
    
    [Reactive]
    public bool IsWaitingFinalStats { get; set; }
    
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