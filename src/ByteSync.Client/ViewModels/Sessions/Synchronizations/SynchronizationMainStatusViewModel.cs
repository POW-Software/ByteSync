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
    private readonly IThemeService _themeService = null!;
    private readonly ILogger<SynchronizationMainStatusViewModel> _logger = null!;
    
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
            // No overlay; icon brush will be updated on state changes
            
            _synchronizationService.SynchronizationProcessData.SynchronizationStart
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest,
                    _synchronizationService.SynchronizationProcessData.SynchronizationEnd)
                .Where(tuple => tuple.First != null && tuple.Second == null && tuple.Third == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple => OnSynchronizationStarted(tuple.First!))
                .DisposeWith(disposables);
            
            _synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest.DistinctUntilChanged()
                .Where(synchronizationAbortRequest => synchronizationAbortRequest != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(synchronizationAbortRequest => OnSynchronizationAbortRequested(synchronizationAbortRequest!))
                .DisposeWith(disposables);
            
            _synchronizationService.SynchronizationProcessData.SynchronizationEnd.DistinctUntilChanged()
                .Where(synchronizationEnd => synchronizationEnd != null)
                .ObserveOn(RxApp.MainThreadScheduler)
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
        IDialogService dialogService, IThemeService themeService, ILogger<SynchronizationMainStatusViewModel> logger) : this()
    {
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
        _dialogService = dialogService;
        _themeService = themeService;
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
    public IBrush? MainIconBrush { get; set; }
    
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
        IsMainCheckVisible = false;
        MainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
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
            MainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
        }
        else if (synchronizationEnd.Status == SynchronizationEndStatuses.Error)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationError;
            MainIcon = "SolidXCircle";
            MainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
        }
        else
        {
            var synchronizationProgress = _synchronizationService.SynchronizationProcessData.SynchronizationProgress.Value;
            var errors = synchronizationProgress?.ErrorActionsCount ?? 0;
            if (errors > 0)
            {
                MainStatus = Resources.ResourceManager.GetString("SynchronizationMain_SynchronizationDoneWithErrors", Resources.Culture)
                             ?? Resources.SynchronizationMain_SynchronizationDone;
                MainIcon = "RegularError";
                MainIconBrush = _themeService.GetBrush("SecondaryButtonBackGround");
            }
            else
            {
                MainStatus = Resources.SynchronizationMain_SynchronizationDone;
                MainIcon = "SolidCheckCircle";
                MainIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
            }
        }
        
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = true;
    }
}