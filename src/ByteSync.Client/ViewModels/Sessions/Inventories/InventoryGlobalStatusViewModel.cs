using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryGlobalStatusViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService = null!;
    private readonly ISessionService _sessionService = null!;
    private readonly IDialogService _dialogService = null!;
    private readonly IThemeService _themeService = null!;
    private readonly ILogger<InventoryGlobalStatusViewModel> _logger = null!;
    
    private InventoryTaskStatus _lastGlobalStatus;
    
    public InventoryGlobalStatusViewModel()
    {
    }
    
    public InventoryGlobalStatusViewModel(IInventoryService inventoryService, ISessionService sessionService,
        IDialogService dialogService, IThemeService themeService,
        IInventoryStatisticsService inventoryStatisticsService, ILogger<InventoryGlobalStatusViewModel> logger)
    {
        _inventoryService = inventoryService;
        _sessionService = sessionService;
        _dialogService = dialogService;
        _themeService = themeService;
        _logger = logger;
        
        AbortInventoryCommand = ReactiveCommand.CreateFromTask(AbortInventory);
        
        this.WhenActivated(disposables =>
        {
            _inventoryService.InventoryProcessData.AnalysisStatus
                .Select(ms => ms == InventoryTaskStatus.Success)
                .ToPropertyEx(this, x => x.ShowGlobalStatistics)
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.GlobalAnalyzeErrors)
                .Select(e => (e ?? 0) > 0)
                .ToPropertyEx(this, x => x.HasErrors)
                .DisposeWith(disposables);
            
            var statusStream = _inventoryService.InventoryProcessData.GlobalMainStatus
                .DistinctUntilChanged()
                .Replay(1)
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
            
            var statsReady = statsStream
                .Select(s => s != null)
                .StartWith(false)
                .DistinctUntilChanged()
                .Replay(1)
                .RefCount();
            statsReady.Subscribe(_ => { }).DisposeWith(disposables);
            
            // statusStream
            //     .Select(st => st == InventoryTaskStatus.Running)
            //     .DistinctUntilChanged()
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .ToPropertyEx(this, x => x.IsInventoryRunning)
            //     .DisposeWith(disposables);
            
            var inProgressUiStream = statusStream
                .CombineLatest(statsReady,
                    (st, ready) => st is InventoryTaskStatus.Pending or InventoryTaskStatus.Running ||
                                   (st == InventoryTaskStatus.Success && !ready))
                .DistinctUntilChanged();
            inProgressUiStream
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsInventoryInProgress)
                .DisposeWith(disposables);
            
            statusStream
                .CombineLatest(statsReady, (st, ready) => st == InventoryTaskStatus.Success && !ready)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(v => IsWaitingFinalStats = v)
                .DisposeWith(disposables);
            
            statusStream
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(st => _lastGlobalStatus = st)
                .DisposeWith(disposables);
            
            _themeService.SelectedTheme
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { UpdateGlobalMainIconBrush(); })
                .DisposeWith(disposables);
            
            var nonSuccessVisual = statusStream
                .Where(st => st is InventoryTaskStatus.Cancelled or InventoryTaskStatus.Error or InventoryTaskStatus.NotLaunched)
                .Select(st => (Icon: "SolidXCircle", Text: st switch
                {
                    InventoryTaskStatus.Cancelled => Resources.InventoryProcess_InventoryCancelled,
                    InventoryTaskStatus.Error => Resources.InventoryProcess_InventoryError,
                    _ => Resources.InventoryProcess_InventoryError
                }, BrushKey: "MainSecondaryColor"));
            
            var successVisual = statusStream
                .Select(st => st == InventoryTaskStatus.Success
                    ? statsStream.Where(s => s != null).Take(1).Select(s =>
                    {
                        if (s!.Errors is > 0)
                        {
                            var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                                       ?? Resources.InventoryProcess_InventorySuccess;
                            
                            return (Icon: "RegularError", Text: text, BrushKey: "MainSecondaryColor");
                        }
                        
                        return (Icon: "SolidCheckCircle", Text: Resources.InventoryProcess_InventorySuccess,
                            BrushKey: "HomeCloudSynchronizationBackGround");
                    })
                    : Observable.Empty<(string Icon, string Text, string BrushKey)>())
                .Switch();
            
            var successOnStats = statsStream
                .Where(s => s != null)
                .WithLatestFrom(statusStream, (s, st) => (s: s!, st))
                .Where(t => t.st == InventoryTaskStatus.Success)
                .Select(t =>
                {
                    if (t.s.Errors is > 0)
                    {
                        var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                                   ?? Resources.InventoryProcess_InventorySuccess;
                        
                        return (Icon: "RegularError", Text: text, BrushKey: "MainSecondaryColor");
                    }
                    
                    return (Icon: "SolidCheckCircle", Text: Resources.InventoryProcess_InventorySuccess,
                        BrushKey: "HomeCloudSynchronizationBackGround");
                });
            
            var visuals = Observable.Merge(nonSuccessVisual, successVisual, successOnStats);
            visuals
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(v =>
                {
                    GlobalMainIcon = v.Icon;
                    GlobalMainStatusText = v.Text;
                    GlobalMainIconBrush = _themeService.GetBrush(v.BrushKey);
                })
                .DisposeWith(disposables);
            
            inventoryStatisticsService.Statistics
                .WithLatestFrom(statusStream, (s, st) => (s, st))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple =>
                {
                    var s = tuple.s;
                    GlobalTotalAnalyzed = s?.TotalAnalyzed;
                    GlobalProcessedSize = s?.ProcessedSize;
                    GlobalAnalyzeSuccess = s?.Success;
                    GlobalAnalyzeErrors = s?.Errors;
                    
                    if (tuple.st == InventoryTaskStatus.Success && s != null)
                    {
                        if (s.Errors is > 0)
                        {
                            var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                                       ?? Resources.InventoryProcess_InventorySuccess;
                            GlobalMainIcon = "RegularError";
                            GlobalMainStatusText = text;
                            GlobalMainIconBrush = _themeService.GetBrush("MainSecondaryColor");
                        }
                        else
                        {
                            GlobalMainIcon = "SolidCheckCircle";
                            GlobalMainStatusText = Resources.InventoryProcess_InventorySuccess;
                            GlobalMainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                        }
                    }
                })
                .DisposeWith(disposables);
            
            statusStream
                .Where(st => st == InventoryTaskStatus.Running)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { GlobalMainStatusText = Resources.InventoryProcess_InventoryRunning; })
                .DisposeWith(disposables);
            
            inProgressUiStream
                .Where(x => !x)
                .WithLatestFrom(statusStream, (_, st) => st)
                .Where(st => st == InventoryTaskStatus.Success)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    var errors = GlobalAnalyzeErrors ?? 0;
                    if (errors > 0)
                    {
                        var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                                   ?? Resources.InventoryProcess_InventorySuccess;
                        GlobalMainIcon = "RegularError";
                        GlobalMainStatusText = text;
                        GlobalMainIconBrush = _themeService.GetBrush("MainSecondaryColor");
                    }
                    else
                    {
                        GlobalMainIcon = "SolidCheckCircle";
                        GlobalMainStatusText = Resources.InventoryProcess_InventorySuccess;
                        GlobalMainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                    }
                })
                .DisposeWith(disposables);
            
            sessionPreparation
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    GlobalTotalAnalyzed = null;
                    GlobalProcessedSize = null;
                    GlobalAnalyzeSuccess = null;
                    GlobalAnalyzeErrors = null;
                    GlobalMainIcon = "None";
                    GlobalMainStatusText = string.Empty;
                    GlobalMainIconBrush = null;
                })
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.GlobalAnalyzeErrors)
                .Where(e => e != null)
                .WithLatestFrom(statusStream, (e, st) => (e: e!.Value, st))
                .Where(t => t.st == InventoryTaskStatus.Success)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(t =>
                {
                    if (t.e > 0)
                    {
                        var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                                   ?? Resources.InventoryProcess_InventorySuccess;
                        GlobalMainIcon = "RegularError";
                        GlobalMainStatusText = text;
                        GlobalMainIconBrush = _themeService.GetBrush("MainSecondaryColor");
                    }
                    else
                    {
                        GlobalMainIcon = "SolidCheckCircle";
                        GlobalMainStatusText = Resources.InventoryProcess_InventorySuccess;
                        GlobalMainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                    }
                })
                .DisposeWith(disposables);
        });
    }
    
    public ReactiveCommand<Unit, Unit> AbortInventoryCommand { get; set; } = null!;
    
    // public extern bool IsInventoryRunning { [ObservableAsProperty] get; }
    //
    // public extern InventoryTaskStatus AnalysisStatus { [ObservableAsProperty] get; }
    
    public extern bool IsInventoryInProgress { [ObservableAsProperty] get; }
    
    public extern bool ShowGlobalStatistics { [ObservableAsProperty] get; }
    
    [Reactive]
    public int? GlobalTotalAnalyzed { get; set; }
    
    [Reactive]
    public long? GlobalProcessedSize { get; set; }
    
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
    
    private void UpdateGlobalMainIconBrush()
    {
        switch (_lastGlobalStatus)
        {
            case InventoryTaskStatus.Error:
            case InventoryTaskStatus.Cancelled:
            case InventoryTaskStatus.NotLaunched:
                GlobalMainIconBrush = _themeService.GetBrush("MainSecondaryColor");
                
                break;
            case InventoryTaskStatus.Success:
                var errors = GlobalAnalyzeErrors ?? 0;
                GlobalMainIconBrush = errors > 0
                    ? _themeService.GetBrush("MainSecondaryColor")
                    : _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                
                break;
            case InventoryTaskStatus.Pending:
            case InventoryTaskStatus.Running:
            default:
                GlobalMainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                
                break;
        }
    }
    
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