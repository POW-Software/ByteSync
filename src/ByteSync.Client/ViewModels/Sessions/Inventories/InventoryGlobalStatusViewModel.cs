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
        
        ViewForMixins.WhenActivated((IActivatableViewModel)this, (CompositeDisposable disposables) =>
        {
            SetupBasicProperties(disposables);
            
            var streams = CreateStreams(inventoryStatisticsService, disposables);
            
            SetupProgressTracking(streams, disposables);
            SetupStatusTracking(streams, disposables);
            SetupThemeHandling(disposables);
            SetupVisualElements(streams, disposables);
            SetupStatisticsSubscription(inventoryStatisticsService, streams, disposables);
            SetupRunningStatusText(streams, disposables);
            SetupProgressCompletionHandling(streams, disposables);
            SetupSessionPreparationReset(streams, disposables);
            SetupErrorHandling(streams, disposables);
        });
    }
    
    public ReactiveCommand<Unit, Unit> AbortInventoryCommand { get; set; } = null!;
    
    public extern bool IsInventoryInProgress { [ObservableAsProperty] get; }
    
    public extern bool ShowGlobalStatistics { [ObservableAsProperty] get; }
    
    [Reactive]
    public int? GlobalTotalAnalyzed { get; set; }
    
    [Reactive]
    public long? GlobalProcessedVolume { get; set; }
    
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
    
    private void SetupBasicProperties(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.AnalysisStatus
            .Select(ms => ms == InventoryTaskStatus.Success)
            .ToPropertyEx(this, x => x.ShowGlobalStatistics)
            .DisposeWith(disposables);
        
        this.WhenAnyValue(x => x.GlobalAnalyzeErrors)
            .Select(e => (e ?? 0) > 0)
            .ToPropertyEx(this, x => x.HasErrors)
            .DisposeWith(disposables);
    }
    
    private ReactiveStreams CreateStreams(IInventoryStatisticsService inventoryStatisticsService, CompositeDisposable disposables)
    {
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
        
        var inProgressUiStream = statusStream
            .CombineLatest(statsReady,
                (st, ready) => st is InventoryTaskStatus.Pending or InventoryTaskStatus.Running ||
                               (st == InventoryTaskStatus.Success && !ready))
            .DistinctUntilChanged();
        
        return new ReactiveStreams(statusStream, sessionPreparation, statsStream, statsReady, inProgressUiStream);
    }
    
    private void SetupProgressTracking(ReactiveStreams streams, CompositeDisposable disposables)
    {
        streams.InProgressUiStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.IsInventoryInProgress)
            .DisposeWith(disposables);
        
        streams.StatusStream
            .CombineLatest(streams.StatsReady, (st, ready) => st == InventoryTaskStatus.Success && !ready)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => IsWaitingFinalStats = v)
            .DisposeWith(disposables);
    }
    
    private void SetupStatusTracking(ReactiveStreams streams, CompositeDisposable disposables)
    {
        streams.StatusStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(st => _lastGlobalStatus = st)
            .DisposeWith(disposables);
    }
    
    private void SetupThemeHandling(CompositeDisposable disposables)
    {
        _themeService.SelectedTheme
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { UpdateGlobalMainIconBrush(); })
            .DisposeWith(disposables);
    }
    
    private void SetupVisualElements(ReactiveStreams streams, CompositeDisposable disposables)
    {
        var nonSuccessVisual = CreateNonSuccessVisual(streams.StatusStream);
        var successVisual = CreateSuccessVisual(streams);
        var successOnStats = CreateSuccessOnStatsVisual(streams);
        
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
    }
    
    private IObservable<(string Icon, string Text, string BrushKey)> CreateNonSuccessVisual(
        IObservable<InventoryTaskStatus> statusStream)
    {
        return statusStream
            .Where(st => st is InventoryTaskStatus.Cancelled or InventoryTaskStatus.Error or InventoryTaskStatus.NotLaunched)
            .Select(st => (Icon: "SolidXCircle", Text: st switch
            {
                InventoryTaskStatus.Cancelled => Resources.InventoryProcess_InventoryCancelled,
                InventoryTaskStatus.Error => Resources.InventoryProcess_InventoryError,
                _ => Resources.InventoryProcess_InventoryError
            }, BrushKey: "MainSecondaryColor"));
    }
    
    private IObservable<(string Icon, string Text, string BrushKey)> CreateSuccessVisual(ReactiveStreams streams)
    {
        return streams.StatusStream
            .Select(st => st == InventoryTaskStatus.Success
                ? streams.StatsStream.Where(s => s != null).Take(1).Select(s => GetSuccessVisualState(s!.Errors))
                : Observable.Empty<(string Icon, string Text, string BrushKey)>())
            .Switch();
    }
    
    private IObservable<(string Icon, string Text, string BrushKey)> CreateSuccessOnStatsVisual(ReactiveStreams streams)
    {
        return streams.StatsStream
            .Where(s => s != null)
            .WithLatestFrom(streams.StatusStream, (s, st) => (s: s!, st))
            .Where(t => t.st == InventoryTaskStatus.Success)
            .Select(t => GetSuccessVisualState(t.s.Errors));
    }
    
    private (string Icon, string Text, string BrushKey) GetSuccessVisualState(int? errors)
    {
        if (errors is > 0)
        {
            var text = Resources.ResourceManager.GetString("InventoryProcess_InventorySuccessWithErrors", Resources.Culture)
                       ?? Resources.InventoryProcess_InventorySuccess;
            
            return (Icon: "RegularError", Text: text, BrushKey: "MainSecondaryColor");
        }
        
        return (Icon: "SolidCheckCircle", Text: Resources.InventoryProcess_InventorySuccess,
            BrushKey: "HomeCloudSynchronizationBackGround");
    }
    
    private void SetupStatisticsSubscription(IInventoryStatisticsService inventoryStatisticsService,
        ReactiveStreams streams, CompositeDisposable disposables)
    {
        inventoryStatisticsService.Statistics
            .WithLatestFrom(streams.StatusStream, (s, st) => (s, st))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(tuple =>
            {
                UpdateStatisticsValues(tuple.s);
                
                if (tuple.st == InventoryTaskStatus.Success && tuple.s != null)
                {
                    ApplySuccessState(tuple.s.Errors);
                }
            })
            .DisposeWith(disposables);
    }
    
    private void UpdateStatisticsValues(InventoryStatistics? stats)
    {
        GlobalTotalAnalyzed = stats?.TotalAnalyzed;
        GlobalProcessedVolume = stats?.ProcessedVolume;
        GlobalAnalyzeSuccess = stats?.Success;
        GlobalAnalyzeErrors = stats?.Errors;
    }
    
    private void ApplySuccessState(int? errors)
    {
        if (errors is > 0)
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
    
    private void SetupRunningStatusText(ReactiveStreams streams, CompositeDisposable disposables)
    {
        streams.StatusStream
            .Where(st => st == InventoryTaskStatus.Running)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { GlobalMainStatusText = Resources.InventoryProcess_InventoryRunning; })
            .DisposeWith(disposables);
    }
    
    private void SetupProgressCompletionHandling(ReactiveStreams streams, CompositeDisposable disposables)
    {
        streams.InProgressUiStream
            .Where(x => !x)
            .WithLatestFrom(streams.StatusStream, (_, st) => st)
            .Where(st => st == InventoryTaskStatus.Success)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                var errors = GlobalAnalyzeErrors ?? 0;
                ApplySuccessState(errors);
            })
            .DisposeWith(disposables);
    }
    
    private void SetupSessionPreparationReset(ReactiveStreams streams, CompositeDisposable disposables)
    {
        streams.SessionPreparation
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ResetStatistics())
            .DisposeWith(disposables);
    }
    
    private void ResetStatistics()
    {
        GlobalTotalAnalyzed = null;
        GlobalProcessedVolume = null;
        GlobalAnalyzeSuccess = null;
        GlobalAnalyzeErrors = null;
        GlobalMainIcon = "None";
        GlobalMainStatusText = string.Empty;
        GlobalMainIconBrush = null;
    }
    
    private void SetupErrorHandling(ReactiveStreams streams, CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.GlobalAnalyzeErrors)
            .Where(e => e != null)
            .WithLatestFrom(streams.StatusStream, (e, st) => (e: e!.Value, st))
            .Where(t => t.st == InventoryTaskStatus.Success)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(t => ApplySuccessState(t.e))
            .DisposeWith(disposables);
    }
    
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
    
    private record ReactiveStreams(
        IObservable<InventoryTaskStatus> StatusStream,
        IObservable<SessionStatus> SessionPreparation,
        IObservable<InventoryStatistics?> StatsStream,
        IObservable<bool> StatsReady,
        IObservable<bool> InProgressUiStream);
}