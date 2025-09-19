using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Synchronizations;

public class SynchronizationMainStatusViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService = null!;
    private readonly ISynchronizationService _synchronizationService = null!;
    private readonly IDialogService _dialogService = null!;
    private readonly ILogger<SynchronizationMainStatusViewModel> _logger = null!;
    private readonly IThemeService _themeService = null!;
    
    public SynchronizationMainStatusViewModel()
    {
    #if DEBUG
        MainStatus = "MainStatus";
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = false;
    #endif
        if (Design.IsDesignMode)
        {
            IsSynchronizationRunning = true;
            IsMainCheckVisible = true;
        }
        
        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
        IsSynchronizationRunning = false;
        
        AbortSynchronizationCommand = ReactiveCommand.CreateFromTask(AbortSynchronization);
        
        this.WhenActivated(disposables =>
        {
            if (_themeService != null)
            {
                _themeService.SelectedTheme
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => { ErrorOverlayBrush = _themeService.GetBrush("StatusSecondaryBackGroundBrush"); })
                    .DisposeWith(disposables);
            }
            
            _synchronizationService.SynchronizationProcessData.SynchronizationStart
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest,
                    _synchronizationService.SynchronizationProcessData.SynchronizationEnd)
                .Where(tuple => tuple.First != null && tuple.Second == null && tuple.Third == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple => OnSynchronizationStarted(tuple.First!))
                .DisposeWith(disposables);
            
            _synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest.DistinctUntilChanged()
                .Where(synchronizationAbortRequest => synchronizationAbortRequest != null)
                .Subscribe(synchronizationAbortRequest => OnSynchronizationAbortRequested(synchronizationAbortRequest!))
                .DisposeWith(disposables);
            
            _synchronizationService.SynchronizationProcessData.SynchronizationEnd.DistinctUntilChanged()
                .Where(synchronizationEnd => synchronizationEnd != null)
                .Subscribe(synchronizationEnd => OnSynchronizationEnded(synchronizationEnd!))
                .DisposeWith(disposables);
            
            _sessionService.SessionStatusObservable
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationStart)
                .Select(tuple =>
                    !tuple.First.In(SessionStatus.None, SessionStatus.Preparation, SessionStatus.Comparison,
                        SessionStatus.CloudSessionCreation, SessionStatus.CloudSessionJunction, SessionStatus.Inventory)
                    && tuple.Second != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.HasSynchronizationStarted)
                .DisposeWith(disposables);
        });
    }
    
    public SynchronizationMainStatusViewModel(ISessionService sessionService, ISynchronizationService synchronizationService,
        IDialogService dialogService, ILogger<SynchronizationMainStatusViewModel> logger, IThemeService themeService) : this()
    {
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
        _dialogService = dialogService;
        _logger = logger;
        _themeService = themeService;
        
        ErrorOverlayBrush = _themeService.GetBrush("StatusSecondaryBackGroundBrush");
    }
    
    // Backward-compatible constructor (without IThemeService)
    public SynchronizationMainStatusViewModel(ISessionService sessionService, ISynchronizationService synchronizationService,
        IDialogService dialogService, ILogger<SynchronizationMainStatusViewModel> logger) : this()
    {
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
        _dialogService = dialogService;
        _logger = logger;
    }
    
    public ReactiveCommand<Unit, Unit> AbortSynchronizationCommand { get; }
    
    [Reactive]
    public bool IsSynchronizationRunning { get; set; }
    
    [Reactive]
    public bool IsMainProgressRingVisible { get; set; }
    
    [Reactive]
    public bool IsMainCheckVisible { get; set; }
    
    [Reactive]
    public bool IsErrorOverlayVisible { get; set; }
    
    [Reactive]
    public IBrush? ErrorOverlayBrush { get; set; }
    
    [Reactive]
    public string? ErrorOverlayTooltip { get; set; }
    
    [Reactive]
    public string MainIcon { get; set; }
    
    [Reactive]
    public string MainStatus { get; set; }
    
    public extern bool HasSynchronizationStarted { [ObservableAsProperty] get; }
    
    private async Task AbortSynchronization()
    {
        try
        {
            var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
                nameof(Resources.SynchronizationMain_AbortSynchronization_Title),
                nameof(Resources.SynchronizationMain_AbortSynchronization_Message));
            messageBoxViewModel.ShowYesNo = true;
            var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
            
            if (result == MessageBoxResult.Yes)
            {
                await _synchronizationService.AbortSynchronization();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SynchronizationMainStatusViewModel.AbortSynchronization");
        }
    }
    
    private void OnSynchronizationStarted(Synchronization _)
    {
        IsSynchronizationRunning = true;
        HandledActionsReset();
        IsMainCheckVisible = false;
        IsErrorOverlayVisible = false;
        ErrorOverlayTooltip = null;
        IsMainProgressRingVisible = true;
        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
    }
    
    private void OnSynchronizationAbortRequested(SynchronizationAbortRequest _)
    {
        if (IsSynchronizationRunning)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationAbortRequested;
        }
    }
    
    private void OnSynchronizationEnded(SynchronizationEnd synchronizationEnd)
    {
        IsSynchronizationRunning = false;
        
        if (synchronizationEnd.Status == SynchronizationEndStatuses.Abortion)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationAborted;
            MainIcon = "SolidXCircle";
            IsErrorOverlayVisible = false;
        }
        else if (synchronizationEnd.Status == SynchronizationEndStatuses.Error)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationError;
            MainIcon = "SolidXCircle";
            IsErrorOverlayVisible = false;
            ErrorOverlayTooltip = null;
        }
        else
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationDone;
            MainIcon = "SolidCheckCircle";
            var synchronizationProgress = _synchronizationService.SynchronizationProcessData.SynchronizationProgress.Value;
            var errors = synchronizationProgress?.ErrorActionsCount ?? 0;
            IsErrorOverlayVisible = errors > 0;
            if (errors > 0)
            {
                var key = errors == 1
                    ? "SynchronizationMain_ErrorEncounteredTemplate"
                    : "SynchronizationMain_ErrorsEncounteredTemplate";
                var template = Resources.ResourceManager.GetString(key, Resources.Culture);
                ErrorOverlayTooltip = string.Format(template ?? "{0}", errors);
            }
            else
            {
                ErrorOverlayTooltip = null;
            }
        }
        
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = true;
    }
    
    private void HandledActionsReset()
    {
    }
}