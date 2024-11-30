using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
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
            // HasSynchronizationStarted = true;
            
            IsMainCheckVisible = true;
            
            // var localizationService = Locator.Current.GetService<ILocalizationService>()!;
            // WaitingForSynchronizationStartMessage = String.Format(localizationService[nameof(Resources.SynchronizationMain_WaitingForClientATemplate)], "Machine A avec un nom long");
        }

        ProcessedVolume = 0;
        ExchangedVolume = 0;

        EstimatedEndDateTimeLabel = Resources.SynchronizationMain_EstimatedEnd;

        // SynchronizationStartedEvent = new ManualResetEvent(false);
    }

    public SynchronizationMainViewModel(ICloudSessionEventsHub cloudSessionEventsHub, 
        ISessionService sessionService, ILocalizationService localizationService, ISynchronizationService synchronizationService,
        IDialogService dialogService, IAtomicActionRepository atomicActionRepository,
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

        // SharedAtomicActions = new ObservableCollection<SharedAtomicAction>();

        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
        // AreDetailsVisible = false;
        // HasSynchronizationStarted = false;
        IsSynchronizationRunning = false;

        StartSynchronizationError = errorViewModel;


        // Pour déterminer si on peut lancer la synchronisation:
        //   - La synchro doit être arrêtée et la session crée par moi
        //   - On écoute les changements sur x.SharedSynchronizationActions (changement de référence)
        //              et sur x.SharedSynchronizationActions.Count (nombre d'éléments)
        // var canStartSynchronization = this
        //     .WhenAnyValue(x => x.HasSynchronizationStarted, x => x.IsCloudSession, x => x.IsCloudSessionCreatedByMe,
        //         x => x.SharedAtomicActions, x => x.SharedAtomicActions!.Count, x => x.IsInventoryError,
        //         ComputeCanStartSynchronization)
        //     .ObserveOn(RxApp.MainThreadScheduler);

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
        

        // this.WhenAnyValue(x => x.StartDateTime)
        //     .Subscribe(_ => UpdateProcessStartInfo());
            
        // this.WhenAnyValue(x => x.ElapsedTime)
        //     .Subscribe(_ => UpdateElapsedTimeInfo());
            
        // this.WhenAnyValue(x => x.EstimatedProcessEnd)
        //     .Subscribe(_ =>
        //     {
        //         UpdateRemainingTimeInfo();
        //         UpdateEstimatedProcessEndInfo();
        //     });
        
        this.WhenActivated(disposables =>
        {
            // Observable.FromEventPattern<SynchronizationStartedEventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SynchronizationStarted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSynchronizationStarted(evt.EventArgs))
            //     .DisposeWith(disposables);
            
            // _synchronizationService.SynchronizationProcessData.SynchronizationStart
            //     .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest, 
            //         _synchronizationService.SynchronizationProcessData.SynchronizationEnd, 
            //         (synchronizationStart, synchronizationAbortRequest, synchronizationEnd) => 
            //             synchronizationAbortRequest == null && synchronizationEnd == null)
            //     .Subscribe(_ => OnSynchronizationStarted(evt.EventArgs))
            //     .DisposeWith(disposables);
            
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
            
            // Observable.FromEventPattern<GenericEventArgs<SynchronizationProgressInfo>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SynchronizationProgressChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSynchronizationProgressChanged(evt.EventArgs.Value))
            //     .DisposeWith(disposables);
            
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
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.MemberQuittedSession))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnMemberQuittedSession())
            //     .DisposeWith(disposables);
            
            // todo 090523
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionResetted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnSessionResetted())
            //     .DisposeWith(disposables);
            
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
            /* // todo  040423
            
            IsInventoryError = _sessionDataHolder.GetLocalInventoryStatus() != LocalInventoryGlobalStatus.Finished &&
                                   _sessionDataHolder.GetOtherSessionMembers()!.Select(m => m.LocalInventoryGlobalStatus)
                                   .All(s => s == LocalInventoryGlobalStatus.Finished);
            */

            
            
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

            // SharedAtomicActions = _synchronizationActionsService.SharedAtomicActions2;
            
            // if (_sessionService is { IsLocalSession: true, CurrentRunSessionProfileInfo.AutoStartsSynchronization: true })
            // {
            //     await Task.Delay(TimeSpan.FromSeconds(3));
            //     await StartSynchronization();
            // } 
            
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

    private bool ComputeCanStartSynchronization(bool isSyncStarted, bool isCloudSession, bool isCloudSessionCreatedByMe, 
        ObservableCollection<SharedAtomicAction>? actions, int actionsCount, bool isInventoryError)
    {
        var result = !isSyncStarted && (!isCloudSession || isCloudSessionCreatedByMe) && actions != null && actionsCount > 0 && !isInventoryError;

        return result;
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

    // private async void HandleActivation(CompositeDisposable compositeDisposable)
    // {
    //     if (Design.IsDesignMode)
    //     {
    //         return;
    //     }
    //     
    //     IsCloudSession = _sessionService.IsCloudSession;
    //     IsCloudSessionCreatedByMe = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;
    //     IsSessionCreatedByMe = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;
    //     IsProfileSessionSynchronization = _sessionService.CurrentRunSessionProfileInfo is { AutoStartsSynchronization: true };
    //
    //         IsInventoryError = true;
    //     /* // todo  040423
    //     
    //     IsInventoryError = _sessionDataHolder.GetLocalInventoryStatus() != LocalInventoryGlobalStatus.Finished &&
    //                            _sessionDataHolder.GetOtherSessionMembers()!.Select(m => m.LocalInventoryGlobalStatus)
    //                            .All(s => s == LocalInventoryGlobalStatus.Finished);
    //     */
    //
    //     
    //     
    //     if (IsCloudSession)
    //     {
    //         if (IsProfileSessionSynchronization)
    //         {
    //             WaitingForSynchronizationStartMessage = _localizationService[nameof(Resources.SynchronizationMain_WaitingForAutomaticStart_CloudSession)];
    //         }
    //         else if (!IsCloudSessionCreatedByMe)
    //         {
    //             var creatorMachineName = _sessionMemberRepository.Elements.First().Endpoint.MachineName;
    //             WaitingForSynchronizationStartMessage = String.Format(_localizationService[nameof(Resources.SynchronizationMain_WaitingForClientATemplate)], creatorMachineName);
    //         }
    //     }
    //     else
    //     {
    //         if (IsProfileSessionSynchronization)
    //         {
    //             WaitingForSynchronizationStartMessage = _localizationService[nameof(Resources.SynchronizationMain_WaitingForAutomaticStart_LocalSession)];
    //         }
    //     }
    //
    //     // SharedAtomicActions = _synchronizationActionsService.SharedAtomicActions2;
    //     
    //     if (_sessionService is { IsLocalSession: true, CurrentRunSessionProfileInfo.AutoStartsSynchronization: true })
    //     {
    //         await Task.Delay(TimeSpan.FromSeconds(3));
    //         await StartSynchronization();
    //     } 
    //     
    //     var remainingTimeComputer = _timeTrackingCache
    //         .GetRemainingTimeComputer(_sessionService.SessionId!, RemainingTimeComputerType.Synchronization)
    //         .Result;
    //     remainingTimeComputer.RemainingTime
    //         .Subscribe(remainingTime =>
    //         {
    //             RemainingTime = remainingTime.RemainingTime;
    //             ElapsedTime = remainingTime.ElapsedTime;
    //             EstimatedEndDateTime = remainingTime.EstimatedEndDateTime;
    //             StartDateTime = remainingTime.StartDateTime;
    //         })
    //         .DisposeWith(compositeDisposable);
    // }
    
    public ViewModelActivator Activator { get; }
    
    public ReactiveCommand<Unit, Unit> StartSynchronizationCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> AbortSynchronizationCommand { get; set; }

    // private RemainingTimeComputer? RemainingTimeComputer { get; set; }

    // [Reactive]
    // public bool AreDetailsVisible { get; set; }
    
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

    // [Reactive]
    // public string ProcessStartInfo { get; set; }

    [Reactive]
    public TimeSpan ElapsedTime { get; set; }
    //
    // [Reactive]
    // public string ElapsedTimeInfo { get; set; }

    // [Reactive]
    // public DateTime? EstimatedProcessEnd { get; set; }

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

    // [Reactive]
    // public long TotalVolume { get; set; }
    
    public extern long TotalVolume { [ObservableAsProperty] get; }

    [Reactive]
    public long ExchangedVolume { get; set; }
    
    // [Reactive]
    // public RemainingTimeData RemainingTimeData { get; set; }

    [Reactive]
    public string WaitingForSynchronizationStartMessage { get; set; }

    // [Reactive]
    // public ObservableCollection<SharedAtomicAction>? SharedAtomicActions { get; set; }
    
    [Reactive]
    public ErrorViewModel StartSynchronizationError { get; set; }
    
    [Reactive]
    public bool HasSessionBeenRestarted { get; set; }
    
    public extern bool ShowStartSynchronizationObservable { [ObservableAsProperty] get; }
    
    public extern bool ShowWaitingForSynchronizationStartObservable { [ObservableAsProperty] get; }
    
    public extern bool HasSynchronizationStarted { [ObservableAsProperty] get; }

    // private ManualResetEvent SynchronizationStartedEvent { get; set; }
    
    private long? LastVersion { get; set; }

    // private void UpdateElapsedTimeInfo()
    // {
    //     // https://docs.microsoft.com/fr-fr/dotnet/standard/base-types/standard-timespan-format-strings
    //     //ElapsedTimeInfo = ElapsedTime.ToString("[-][d.]hh:mm:ss", TranslationSource.GetInstance().CurrentCulture);
    //     ElapsedTimeInfo = ElapsedTime.ToString(@"hh\:mm\:ss", _localizationService.CurrentCultureDefinition.CultureInfo);
    // }
        
    // private void UpdateRemainingTimeInfo()
    // {
    //     if (EstimatedProcessEnd != null)
    //     {
    //         var delay = EstimatedProcessEnd.Value - DateTime.Now.Trim(TimeSpan.TicksPerSecond);
    //
    //         if (delay < TimeSpan.Zero)
    //         {
    //             delay = TimeSpan.Zero;
    //         }
    //
    //         // https://docs.microsoft.com/fr-fr/dotnet/standard/base-types/standard-timespan-format-strings
    //         //RemainingTimeInfo = delay.ToString("[-][d.]hh:mm:ss", TranslationSource.GetInstance().CurrentCulture);
    //         RemainingTimeInfo = delay.ToString(@"hh\:mm\:ss", _localizationService.CurrentCultureDefinition.CultureInfo);
    //     }
    //     else
    //     {
    //         RemainingTimeInfo = "";
    //     }
    // }

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

    // private void UpdateProcessStartInfo()
    // {
    //     ProcessStartInfo = StartDateTime.ToString("G", _localizationService.CurrentCultureDefinition.CultureInfo);
    // }

    // private void UpdateEstimatedProcessEndInfo()
    // {
    //     if (EstimatedProcessEnd != null)
    //     {
    //         EstimatedProcessEndInfo = EstimatedProcessEnd.Value.ToString("G", _localizationService.CurrentCultureDefinition.CultureInfo);
    //     }
    //     else
    //     {
    //         EstimatedProcessEndInfo = "";
    //     }
    // }

    private void OnSynchronizationStarted(Synchronization synchronizationStart)
    {
        // HasSynchronizationStarted = true;
        StartDateTime = synchronizationStart.Started.LocalDateTime;
        TreatableActions = _synchronizationService.SynchronizationProcessData.TotalActionsToProcess;


        // var aggregation = _synchronizationActionsService.SharedActionsGroups.Connect().ToCollection()
        //     .Select(query =>
        //     {
        //         var sum = query.Sum(ssa => ssa.Size.GetValueOrDefault());
        //
        //         return sum;
        //     });
        //
        // long totalSize = _synchronizationActionsService.SharedActionsGroups.Sum(ssa => ssa.Size.GetValueOrDefault());
        // TotalVolume = totalSize;
        
        // UpdateCurrentAction(null);
        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
        
        IsSynchronizationRunning = true;

        HandledActions = 0;
        Errors = 0;

        // RemainingTimeData = new RemainingTimeData();
        // RemainingTimeComputer = new RemainingTimeComputer(RemainingTimeData);
        // // RemainingTimeComputer.EstimatedDateTimeChanged += RemainingTimeComputer_OnEstimatedDateTimeChanged;
        // RemainingTimeComputer.SetDataToHandle(TotalVolume);
        // RemainingTimeComputer.Start(DateTime.Now);


        // _timer = new DispatcherTimer();
        // _timer.Interval = TimeSpan.FromSeconds(1);
        // _timer.Tick += Timer_Tick;
        // _timer.Start();

        ElapsedTime = TimeSpan.Zero;

        IsMainCheckVisible = false;
        IsMainProgressRingVisible = true;
        
        // SynchronizationStartedEvent.Set();
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
        // var timer = _timer;
        // if (timer != null)
        // {
        //     timer.Stop();
        //     timer.IsEnabled = false;
        // }

        // UpdateCurrentAction(null);
        IsSynchronizationRunning = false;
        
        // EstimatedProcessEnd = DateTime.Now;
        // UpdateElapsedTime();
        
        // RemainingTimeComputer?.Stop();

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
        
        // TreatableActions = _sessionDataHolder.SharedActionsGroups?.Count ?? 0;
        // RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
        
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = true;
    }

    // private void UpdateElapsedTime()
    // {
    //     if (EstimatedProcessEnd != null)
    //     {
    //         if (StartDateTime != DateTime.MinValue)
    //         {
    //             ElapsedTime = EstimatedProcessEnd.Value - StartDateTime;
    //         }
    //         else
    //         {
    //             ElapsedTime = TimeSpan.Zero;
    //         }
    //     }
    // }
    
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
                
        // if (!_sessionDataHolder.IsSynchronizationEnded)
        // {
        //     TreatableActions = _sessionDataHolder.SharedActionsGroups?.Count ?? 0;
        //     RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
        // }
                
        // todo RemainingTimeComputer.SetDataHandled 
        // RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
                
        ProcessedVolume = synchronizationProgress.ProcessedVolume;
        ExchangedVolume = synchronizationProgress.ExchangedVolume;
        LastVersion = synchronizationProgress.Version;
        
        
        
        // SynchronizationStartedEvent.WaitOne();

        // Dispatcher.UIThread.InvokeAsync(() =>
        // {
        //     if (LastVersion != null && LastVersion > synchronizationProgress.Version)
        //     {
        //         return;
        //     }
        //         
        //     HandledActions = synchronizationProgress.FinishedActionsCount;
        //     Errors = synchronizationProgress.ErrorActionsCount;
        //         
        //     // if (!_sessionDataHolder.IsSynchronizationEnded)
        //     // {
        //     //     TreatableActions = _sessionDataHolder.SharedActionsGroups?.Count ?? 0;
        //     //     RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
        //     // }
        //         
        //     // todo RemainingTimeComputer.SetDataHandled 
        //     // RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
        //         
        //     ProcessedVolume = synchronizationProgress.ProcessedVolume;
        //     ExchangedVolume = synchronizationProgress.ExchangedVolume;
        //     LastVersion = synchronizationProgress.Version;
        // });
    }

    // private void OnSynchronizationProgressChanged(SynchronizationProgressInfo synchronizationProgress)
    // {
    //     Task.Run(() =>
    //     {
    //         SynchronizationStartedEvent.WaitOne();
    //
    //         Dispatcher.UIThread.InvokeAsync(() =>
    //         {
    //             if (LastVersionNumber != null && LastVersionNumber > synchronizationProgress.VersionNumber)
    //             {
    //                 return;
    //             }
    //             
    //             HandledActions = _synchronizationActionsService.ProgressActionsCount;
    //             Errors = _synchronizationActionsService.ProgressActionsErrorsCount;
    //             
    //             // if (!_sessionDataHolder.IsSynchronizationEnded)
    //             // {
    //             //     TreatableActions = _sessionDataHolder.SharedActionsGroups?.Count ?? 0;
    //             //     RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
    //             // }
    //             
    //             // todo RemainingTimeComputer.SetDataHandled 
    //             // RemainingTimeComputer?.SetDataHandled(synchronizationProgress.ProcessedVolume);
    //             
    //             ProcessedVolume = synchronizationProgress.ProcessedVolume;
    //             ExchangedVolume = synchronizationProgress.ExchangedVolume;
    //             LastVersionNumber = synchronizationProgress.VersionNumber;
    //         });
    //     });
    // }
    
    // todo 090523
    private void OnSessionResetted()
    {
        // HasSynchronizationStarted = false;
        IsSynchronizationRunning = false;
        
        MainStatus = Resources.SynchronizationMain_SynchronizationRunning;
        
        IsMainProgressRingVisible = false;
        IsMainCheckVisible = false;
        
        ProcessedVolume = 0;
        LastVersion = 0;
        // TotalVolume = 0;
        ExchangedVolume = 0;
        
        HasSessionBeenRestarted = true;
    }

    // private void UpdateCurrentAction(SharedActionsGroup? sharedActionsGroup)
    // {
    //     if (sharedActionsGroup != null)
    //     {
    //         var actionsGroupDescriptionBuilder = new SharedActionsGroupDescriptionBuilder(_localizationService);
    //         CurrentAction =
    //             $"'{sharedActionsGroup.LinkingKeyValue}'{Resources.General_Colon} {actionsGroupDescriptionBuilder.GetDescription(sharedActionsGroup)}";
    //     }
    //     else
    //     {
    //         CurrentAction = "";
    //     }
    // }

    // private void RemainingTimeComputer_OnEstimatedDateTimeChanged(DateTime? estimatedEndDateTime)
    // {
    //     EstimatedProcessEnd = estimatedEndDateTime;
    // }

    // private void Timer_Tick(object sender, EventArgs e)
    // {
    //     if (_timer.IsEnabled)
    //     {
    //         _timer.Stop();
    //         _timer.Interval = TimeSpan.FromSeconds(1).Subtract(TimeSpan.FromMilliseconds(DateTime.Now.Millisecond));
    //         _timer.Start();
    //     }
    //
    //     ElapsedTime = DateTime.Now - ProcessStart;
    //     UpdateRemainingTimeInfo();
    // }
    
    // private void OnMemberQuittedSession()
    // {
    //     IsCloudSessionCreatedByMe = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;
    // }
}