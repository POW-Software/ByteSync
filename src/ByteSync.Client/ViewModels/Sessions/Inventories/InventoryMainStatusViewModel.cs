using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
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
            
            // Shared streams
            var statusStream = _inventoryService.InventoryProcessData.GlobalMainStatus
                .DistinctUntilChanged()
                .Publish()
                .RefCount();
            var sessionPreparation = _sessionService.SessionStatusObservable
                .Where(ss => ss == SessionStatus.Preparation)
                .Publish()
                .RefCount();
            var statsStream = sessionPreparation
                .Select(_ => (InventoryStatistics?)null)
                .Merge(inventoryStatisticsService.Statistics)
                .Replay(1)
                .RefCount();
            statsStream.Subscribe(_ => { }).DisposeWith(disposables);
            
            // Waiting flag: on Success, stay true until the first stats emission; otherwise false
            var waitingFinalStats = statusStream
                .Select(st => st == InventoryTaskStatus.Success
                    ? statsStream.Where(s => s != null).Take(1).Select(_ => false).StartWith(true)
                    : Observable.Return(false))
                .Switch()
                .DistinctUntilChanged();
            waitingFinalStats
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(v => IsWaitingFinalStats = v)
                .DisposeWith(disposables);
            
            // IsInventoryRunning directly from status stream
            statusStream
                .Select(st => st == InventoryTaskStatus.Running)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsInventoryRunning)
                .DisposeWith(disposables);
            
            // IsInventoryInProgress computed from status + waiting flag
            statusStream
                .Select(st => st is InventoryTaskStatus.Pending or InventoryTaskStatus.Running)
                .CombineLatest(waitingFinalStats, (rp, wait) => rp || wait)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsInventoryInProgress)
                .DisposeWith(disposables);
            
            // Map to final UI visuals: immediate for non-success; gated on stats for success
            var nonSuccessVisual = statusStream
                .Where(st => st is InventoryTaskStatus.Error or InventoryTaskStatus.Cancelled or InventoryTaskStatus.NotLaunched)
                .Select(st =>
                {
                    var key = $"InventoryProcess_Inventory{st}";
                    var text = Resources.ResourceManager.GetString(key, Resources.Culture) ?? string.Empty;
                    
                    return (Icon: "SolidXCircle", Text: text, BrushKey: "MainSecondaryColor");
                });
            
            var successVisual = statusStream
                .Where(st => st == InventoryTaskStatus.Success)
                .Select(_ => statsStream.Where(s => s != null).Take(1))
                .Switch()
                .Select(s =>
                {
                    var stats = s!;
                    var errors = stats.Errors;
                    if (errors > 0)
                    {
                        _logger.LogWarning("DEBUG - inventory completed with {Errors} errors", errors);
                        var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                                   ?? Resources.InventoryProcess_InventorySuccess;
                        
                        return (Icon: "RegularError", Text: text, BrushKey: "MainSecondaryColor");
                    }
                    else
                    {
                        _logger.LogInformation("DEBUG - inventory completed successfully with no errors");
                        
                        return (Icon: "SolidCheckCircle", Text: Resources.InventoryProcess_InventorySuccess,
                            BrushKey: "HomeCloudSynchronizationBackGround");
                    }
                });
            
            var visuals = Observable.Merge(nonSuccessVisual, successVisual)
                .ObserveOn(RxApp.MainThreadScheduler);
            visuals
                .Subscribe(v =>
                {
                    GlobalMainIcon = v.Icon;
                    GlobalMainStatusText = v.Text;
                    GlobalMainIconBrush = GetBrushSafe(v.BrushKey);
                })
                .DisposeWith(disposables);
            
            sessionPreparation
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    GlobalTotalAnalyzed = null;
                    GlobalAnalyzeSuccess = null;
                    GlobalAnalyzeErrors = null;
                    GlobalMainIcon = "None";
                    GlobalMainStatusText = string.Empty;
                    GlobalMainIconBrush = null;
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
    
    private IBrush? GetBrushSafe(string resourceName)
    {
        var brush = _themeService?.GetBrush(resourceName);
        
        if (brush != null) return brush;
        
        var themeVariant = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        if (Application.Current?.Styles.TryGetResource(resourceName, themeVariant, out var res) == true)
        {
            if (res is IBrush b) return b;
            if (res is Color c) return new SolidColorBrush(c);
        }
        
        if (Application.Current?.Styles.TryGetResource(resourceName, ThemeVariant.Default, out var res2) == true)
        {
            if (res2 is IBrush b2) return b2;
            if (res2 is Color c2) return new SolidColorBrush(c2);
        }
        
        return null;
    }
}