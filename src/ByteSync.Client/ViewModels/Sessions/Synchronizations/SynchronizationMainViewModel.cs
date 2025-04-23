using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Misc;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Synchronizations;

public class SynchronizationMainViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly IDialogService _dialogService;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly ISynchronizationStarter _synchronizationStarter;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly ITimeTrackingCache _timeTrackingCache;
    private readonly ILogger<SynchronizationMainViewModel> _logger;

    public SynchronizationMainViewModel()
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

        ProcessedVolume = 0;
        ExchangedVolume = 0;

        EstimatedEndDateTimeLabel = Resources.SynchronizationMain_EstimatedEnd;
    }

    public SynchronizationMainViewModel(ISessionService sessionService, ILocalizationService localizationService, 
        ISynchronizationService synchronizationService, IDialogService dialogService, IAtomicActionRepository atomicActionRepository,
        ISessionMemberRepository sessionMemberRepository, ErrorViewModel errorViewModel, ISynchronizationStarter synchronizationStarter,
        ISharedActionsGroupRepository sharedActionsGroupRepository, ITimeTrackingCache timeTrackingCache, ILogger<SynchronizationMainViewModel> logger) 
        : this()
    {
        Activator = new ViewModelActivator();

        _sessionService = sessionService;
        _localizationService = localizationService;
        _synchronizationService = synchronizationService;
        _dialogService = dialogService;
        _atomicActionRepository = atomicActionRepository;
        _sessionMemberRepository = sessionMemberRepository;
        _synchronizationStarter = synchronizationStarter;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _timeTrackingCache = timeTrackingCache;
        _logger = logger;
        
        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
        IsSynchronizationRunning = false;

        StartSynchronizationError = errorViewModel;
        var canStartSynchronization = _sessionService.SessionObservable
            .CombineLatest(_atomicActionRepository.ObservableCache.Connect(), _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable,
                (session, atomicActions, isCurrentUserFirstSessionMember) =>
                    (Session: session, AtomicActions: atomicActions, IsCurrentUserFirstSessionMember: isCurrentUserFirstSessionMember))
            // .CombineLatest(_sessionMembersService.IsCurrentUserFirstSessionMemberObservable)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(tuple => tuple.Session is CloudSession && tuple.IsCurrentUserFirstSessionMember && tuple.AtomicActions.Count > 0);
            // .Select(tuple => tuple.First == SessionStatus.Preparation && tuple.Second == null)
        
        StartSynchronizationCommand = ReactiveCommand.CreateFromTask(StartSynchronization, canStartSynchronization);
        
        AbortSynchronizationCommand = ReactiveCommand.CreateFromTask(AbortSynchronization);
        

        this.WhenAnyValue(
                x => x.IsSynchronizationRunning, x => x.IsCloudSession, x => x.IsSessionCreatedByMe, x => x.HasSynchronizationStarted,
                x => x.IsProfileSessionSynchronization, x => x.HasSessionBeenRestarted,
                ComputeShowStartSynchronizationObservable)
            .ToPropertyEx(this, x => x.ShowStartSynchronizationObservable);
        
        this.WhenAnyValue(
                x => x.IsSynchronizationRunning, x => x.IsCloudSession, x => x.IsSessionCreatedByMe, x => x.HasSynchronizationStarted,
                x => x.IsProfileSessionSynchronization, x => x.HasSessionBeenRestarted,
                ComputeShowWaitingForSynchronizationStartObservable)
            .ToPropertyEx(this, x => x.ShowWaitingForSynchronizationStartObservable);
        
        this.WhenActivated(disposables =>
        {
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

            _sharedActionsGroupRepository.ObservableCache.Connect().ToCollection()
                .Select(query =>
                {
                    var sum = query.Sum(ssa => ssa.Size.GetValueOrDefault());

                    return sum;
                })
                .StartWith(0)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.TotalVolume)
                .DisposeWith(disposables);
            
            _synchronizationService.SynchronizationProcessData.SynchronizationProgress
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationStart)
                .Where(tuple => tuple.First != null && tuple.Second != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                // .Where(sp => sp != null)
                // .Select(sp => sp!)
                .Select(tuple => tuple.First!)
                .Subscribe(OnSynchronizationProgressChanged)
                .DisposeWith(disposables);
            
            _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable
                .Subscribe(b => IsSessionCreatedByMe = b);
            
            _sessionService.SessionStatusObservable
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationStart)
                .Select(tuple => 
                    !tuple.First.In(SessionStatus.None, SessionStatus.Preparation, SessionStatus.Comparison, 
                                     SessionStatus.CloudSessionCreation, SessionStatus.CloudSessionJunction, SessionStatus.Inventory)
                    && tuple.Second != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.HasSynchronizationStarted)
                .DisposeWith(disposables);
            
            if (Design.IsDesignMode)
            {
                return;
            }
            
            IsCloudSession = _sessionService.IsCloudSession;
            IsSessionCreatedByMe = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;
            IsProfileSessionSynchronization = _sessionService.CurrentRunSessionProfileInfo is { AutoStartsSynchronization: true };
            IsInventoryError = true;
            
            if (IsCloudSession)
            {
                if (IsProfileSessionSynchronization)
                {
                    WaitingForSynchronizationStartMessage = _localizationService[nameof(Resources.SynchronizationMain_WaitingForAutomaticStart_CloudSession)];
                }
                else if (!IsCloudSessionCreatedByMe)
                {
                    var creatorMachineName = _sessionMemberRepository.Elements.First().MachineName;
                    WaitingForSynchronizationStartMessage = String.Format(_localizationService[nameof(Resources.SynchronizationMain_WaitingForClientATemplate)], creatorMachineName);
                }
            }
            else
            {
                if (IsProfileSessionSynchronization)
                {
                    WaitingForSynchronizationStartMessage = _localizationService[nameof(Resources.SynchronizationMain_WaitingForAutomaticStart_LocalSession)];
                }
            }
            
            var timeTrackingComputer = _timeTrackingCache
                .GetTimeTrackingComputer(_sessionService.SessionId!, TimeTrackingComputerType.Synchronization)
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
        });
            
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = false;
    }

    private bool ComputeShowStartSynchronizationObservable(bool isSynchronizationRunning, bool isCloudSession, bool isSessionCreatedByMe, 
        bool hasSynchronizationStarted, bool isProfileSessionSynchronization, bool hasSessionBeenRestarted)
    {
        var result = (!isProfileSessionSynchronization || (! isCloudSession && hasSessionBeenRestarted)) 
                     && !isSynchronizationRunning 
                     && !hasSynchronizationStarted 
                     && (!isCloudSession || isSessionCreatedByMe);

        return result;
    }
    
    private bool ComputeShowWaitingForSynchronizationStartObservable(bool isSynchronizationRunning, bool isCloudSession, bool isSessionCreatedByMe, 
        bool hasSynchronizationStarted, bool isProfileSessionSynchronization, bool hasSessionBeenRestarted)
    {
        var result = !isSynchronizationRunning 
                     && !hasSynchronizationStarted
                     && ((isProfileSessionSynchronization && !hasSessionBeenRestarted) || (!isSessionCreatedByMe && isCloudSession));
        
        return result;
    }
    
    public ViewModelActivator Activator { get; }
    
    public ReactiveCommand<Unit, Unit> StartSynchronizationCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> AbortSynchronizationCommand { get; set; }
    
    [Reactive]
    public bool IsSynchronizationRunning { get; set; }
    
    [Reactive]
    public bool IsMainProgressRingVisible { get; set; }

    [Reactive]
    public bool IsMainCheckVisible { get; set; }
    
    [Reactive]
    public string MainIcon { get; set; }
    
    [Reactive]
    public bool IsCloudSession { get; set; }
    
    public bool IsCloudSessionCreatedByMe => IsCloudSession && IsSessionCreatedByMe;
    
    [Reactive]
    public bool IsSessionCreatedByMe { get; set; }
    
    [Reactive]
    public bool IsProfileSessionSynchronization { get; set; }
    
    [Reactive]
    public bool IsInventoryError { get; set; }

    [Reactive]
    public string MainStatus { get; set; }

    [Reactive]
    public DateTime? StartDateTime { get; set; }

    [Reactive]
    public TimeSpan ElapsedTime { get; set; }

    [Reactive]
    public TimeSpan? RemainingTime { get; set; }

    [Reactive]
    public string EstimatedEndDateTimeLabel { get; set; }

    [Reactive]
    public DateTime? EstimatedEndDateTime { get; set; }

    [Reactive]
    public long HandledActions { get; set; }

    [Reactive]
    public long? TreatableActions { get; set; }

    [Reactive]
    public long Errors { get; set; }
    
    [Reactive]
    public long ProcessedVolume { get; set; }
    
    public extern long TotalVolume { [ObservableAsProperty] get; }

    [Reactive]
    public long ExchangedVolume { get; set; }

    [Reactive]
    public string WaitingForSynchronizationStartMessage { get; set; }
    
    [Reactive]
    public ErrorViewModel StartSynchronizationError { get; set; }
    
    [Reactive]
    public bool HasSessionBeenRestarted { get; set; }
    
    public extern bool ShowStartSynchronizationObservable { [ObservableAsProperty] get; }
    
    public extern bool ShowWaitingForSynchronizationStartObservable { [ObservableAsProperty] get; }
    
    public extern bool HasSynchronizationStarted { [ObservableAsProperty] get; }
    
    private long? LastVersion { get; set; }

    private async Task StartSynchronization()
    {
        try
        {
            StartSynchronizationError.Clear();

            await _synchronizationStarter.StartSynchronization(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SynchronizationMainViewModel.StartSynchronization");
            
            StartSynchronizationError.SetException(ex);
        }
    }
    
    private async Task AbortSynchronization()
    {
        try
        {
            var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
                nameof(Resources.SynchronizationMain_AbortSynchronization_Title), nameof(Resources.SynchronizationMain_AbortSynchronization_Message));
            messageBoxViewModel.ShowYesNo = true;
            var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

            if (result == MessageBoxResult.Yes)
            {
                await _synchronizationService.AbortSynchronization();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SynchronizationMainViewModel.AbortSynchronization");
        }
    }

    private void OnSynchronizationStarted(Synchronization synchronizationStart)
    {
        StartDateTime = synchronizationStart.Started.LocalDateTime;
        TreatableActions = _synchronizationService.SynchronizationProcessData.TotalActionsToProcess;

        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
        
        IsSynchronizationRunning = true;

        HandledActions = 0;
        Errors = 0;

        ElapsedTime = TimeSpan.Zero;

        IsMainCheckVisible = false;
        IsMainProgressRingVisible = true;
    }
    
    
    private void OnSynchronizationAbortRequested(SynchronizationAbortRequest synchronizationAbortRequest)
    {
        if (IsSynchronizationRunning)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationAbortRequested;
        }
    }
    
    private void OnSynchronizationEnded(SynchronizationEnd synchronizationEnd)
    {
        IsSynchronizationRunning = false;

        EstimatedEndDateTimeLabel = Resources.SynchronizationMain_End;
        
        var synchronizationProgress = _synchronizationService.SynchronizationProcessData.SynchronizationProgress.Value;
        HandledActions = synchronizationProgress?.FinishedActionsCount ?? 0;
        Errors = synchronizationProgress?.ErrorActionsCount ?? 0;
        
        if (synchronizationEnd.Status == SynchronizationEndStatuses.Abortion)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationAborted;
            MainIcon = "SolidXCircle";
        }
        else if (synchronizationEnd.Status == SynchronizationEndStatuses.Error)
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationError;
            MainIcon = "SolidXCircle";
        }
        else
        {
            MainStatus = Resources.SynchronizationMain_SynchronizationDone;
            MainIcon = "SolidCheckCircle";
        }
        
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = true;
    }
    
    private void OnSynchronizationProgressChanged(SynchronizationProgress? synchronizationProgress)
    {
        if (synchronizationProgress == null)
        {
            return;
        }
        
        if (LastVersion != null && LastVersion > synchronizationProgress.Version)
        {
            return;
        }
                
        HandledActions = synchronizationProgress.FinishedActionsCount;
        Errors = synchronizationProgress.ErrorActionsCount;
                
        ProcessedVolume = synchronizationProgress.ProcessedVolume;
        ExchangedVolume = synchronizationProgress.ExchangedVolume;
        LastVersion = synchronizationProgress.Version;
    }
}