using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using ByteSync.Assets.Resources;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Misc;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Synchronizations;

public class SynchronizationBeforeStartViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISynchronizationStarter _synchronizationStarter;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public SynchronizationBeforeStartViewModel()
    {
    }

    public SynchronizationBeforeStartViewModel(ISessionService sessionService, ILocalizationService localizationService,
        ISynchronizationService synchronizationService, ISynchronizationStarter synchronizationStarter,
        IAtomicActionRepository atomicActionRepository, ISessionMemberRepository sessionMemberRepository, ErrorViewModel errorViewModel)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _synchronizationService = synchronizationService;
        _synchronizationStarter = synchronizationStarter;
        _atomicActionRepository = atomicActionRepository;
        _sessionMemberRepository = sessionMemberRepository;

        StartSynchronizationError = errorViewModel;

        var canStartSynchronization = _sessionService.SessionObservable
            .CombineLatest(
                _atomicActionRepository.ObservableCache.Connect().ToCollection(),
                _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable,
                (session, atomicActions, isCurrentUserFirstSessionMember) =>
                    (Session: session, AtomicActions: atomicActions, IsCurrentUserFirstSessionMember: isCurrentUserFirstSessionMember))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(tuple => tuple.Session is CloudSession && tuple.IsCurrentUserFirstSessionMember && tuple.AtomicActions.Count > 0);

        StartSynchronizationCommand = ReactiveCommand.CreateFromTask(StartSynchronization, canStartSynchronization);

        this.WhenActivated(disposables =>
        {
            _synchronizationService.SynchronizationProcessData.SynchronizationStart
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => IsSynchronizationRunning = true)
                .DisposeWith(disposables);

            _synchronizationService.SynchronizationProcessData.SynchronizationEnd
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => IsSynchronizationRunning = false)
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

            this.WhenAnyValue(
                    x => x.IsSynchronizationRunning, x => x.IsCloudSession, x => x.IsSessionCreatedByMe, x => x.HasSynchronizationStarted,
                    x => x.IsProfileSessionSynchronization, x => x.HasSessionBeenRestarted,
                    ComputeShowStartSynchronizationObservable)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.ShowStartSynchronizationObservable)
                .DisposeWith(disposables);

            this.WhenAnyValue(
                    x => x.IsSynchronizationRunning, x => x.IsCloudSession, x => x.IsSessionCreatedByMe, x => x.HasSynchronizationStarted,
                    x => x.IsProfileSessionSynchronization, x => x.HasSessionBeenRestarted,
                    ComputeShowWaitingForSynchronizationStartObservable)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.ShowWaitingForSynchronizationStartObservable)
                .DisposeWith(disposables);
        });

        if (Design.IsDesignMode)
        {
            return;
        }

        IsCloudSession = _sessionService.IsCloudSession;
        IsSessionCreatedByMe = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;
        IsProfileSessionSynchronization = _sessionService.CurrentRunSessionProfileInfo is { AutoStartsSynchronization: true };

        if (IsCloudSession)
        {
            if (IsProfileSessionSynchronization)
            {
                WaitingForSynchronizationStartMessage = _localizationService[nameof(Resources.SynchronizationMain_WaitingForAutomaticStart_CloudSession)];
            }
            else if (!IsCloudSessionCreatedByMe)
            {
                var creatorMachineName = _sessionMemberRepository.Elements.First().MachineName;
                WaitingForSynchronizationStartMessage = string.Format(_localizationService[nameof(Resources.SynchronizationMain_WaitingForClientATemplate)], creatorMachineName);
            }
        }
        else
        {
            if (IsProfileSessionSynchronization)
            {
                WaitingForSynchronizationStartMessage = _localizationService[nameof(Resources.SynchronizationMain_WaitingForAutomaticStart_LocalSession)];
            }
        }
    }

    public ReactiveCommand<Unit, Unit> StartSynchronizationCommand { get; }

    [Reactive]
    public bool IsSynchronizationRunning { get; set; }

    [Reactive]
    public bool IsCloudSession { get; set; }

    [Reactive]
    public bool IsSessionCreatedByMe { get; set; }

    [Reactive]
    public bool IsProfileSessionSynchronization { get; set; }

    [Reactive]
    public bool HasSessionBeenRestarted { get; set; }

    [Reactive]
    public string WaitingForSynchronizationStartMessage { get; set; }

    [Reactive]
    public ErrorViewModel StartSynchronizationError { get; set; }

    public extern bool ShowStartSynchronizationObservable { [ObservableAsProperty] get; }

    public extern bool ShowWaitingForSynchronizationStartObservable { [ObservableAsProperty] get; }

    public extern bool HasSynchronizationStarted { [ObservableAsProperty] get; }

    private bool IsCloudSessionCreatedByMe => IsCloudSession && IsSessionCreatedByMe;

    private bool ComputeShowStartSynchronizationObservable(bool isSynchronizationRunning, bool isCloudSession, bool isSessionCreatedByMe,
        bool hasSynchronizationStarted, bool isProfileSessionSynchronization, bool hasSessionBeenRestarted)
    {
        var result = (!isProfileSessionSynchronization || (!isCloudSession && hasSessionBeenRestarted))
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

    private async Task StartSynchronization()
    {
        try
        {
            StartSynchronizationError.Clear();

            await _synchronizationStarter.StartSynchronization(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SynchronizationBeforeStartViewModel.StartSynchronization");

            StartSynchronizationError.SetException(ex);
        }
    }
}
