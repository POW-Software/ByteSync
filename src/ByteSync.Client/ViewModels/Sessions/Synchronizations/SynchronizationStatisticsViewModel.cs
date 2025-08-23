using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using DynamicData;
using ByteSync.Business.Misc;

namespace ByteSync.ViewModels.Sessions.Synchronizations;

public class SynchronizationStatisticsViewModel : ActivatableViewModelBase
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISessionService _sessionService;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly ITimeTrackingCache _timeTrackingCache;

    private long? LastVersion { get; set; }

    public SynchronizationStatisticsViewModel()
    {
        ProcessedVolume = 0;
        ExchangedVolume = 0;
        EstimatedEndDateTimeLabel = Resources.SynchronizationMain_EstimatedEnd;
    }

    public SynchronizationStatisticsViewModel(ISynchronizationService synchronizationService, ISessionService sessionService,
        ISharedActionsGroupRepository sharedActionsGroupRepository, ITimeTrackingCache timeTrackingCache) : this()
    {
        _synchronizationService = synchronizationService;
        _sessionService = sessionService;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _timeTrackingCache = timeTrackingCache;

        this.WhenActivated(disposables =>
        {
            _synchronizationService.SynchronizationProcessData.SynchronizationStart
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest,
                    _synchronizationService.SynchronizationProcessData.SynchronizationEnd)
                .Where(tuple => tuple.First != null && tuple.Second == null && tuple.Third == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple => OnSynchronizationStarted(tuple.First!))
                .DisposeWith(disposables);

            _synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest,
                    _synchronizationService.SynchronizationProcessData.SynchronizationEnd)
                .Where(tuple => tuple.Second == null && tuple.Third == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple => OnSynchronizationDataTransmitted(tuple.First))
                .DisposeWith(disposables);

            _synchronizationService.SynchronizationProcessData.SynchronizationEnd.DistinctUntilChanged()
                .Where(synchronizationEnd => synchronizationEnd != null)
                .Subscribe(synchronizationEnd => OnSynchronizationEnded(synchronizationEnd!))
                .DisposeWith(disposables);

            _sharedActionsGroupRepository.ObservableCache.Connect().ToCollection()
                .Select(query => query.Sum(ssa => ssa.Size.GetValueOrDefault()))
                .StartWith(0L)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.TotalVolume)
                .DisposeWith(disposables);

            _synchronizationService.SynchronizationProcessData.SynchronizationProgress
                .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationStart)
                .Where(tuple => tuple.First != null && tuple.Second != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(tuple => tuple.First!)
                .Subscribe(OnSynchronizationProgressChanged)
                .DisposeWith(disposables);

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

        IsCloudSession = _sessionService.IsCloudSession;
    }

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
    public bool IsCloudSession { get; set; }

    private void OnSynchronizationStarted(Synchronization synchronizationStart)
    {
        StartDateTime = synchronizationStart.Started.LocalDateTime;
        HandledActions = 0;
        Errors = 0;
        ElapsedTime = TimeSpan.Zero;
    }

    private void OnSynchronizationDataTransmitted(bool tupleFirst)
    {
        TreatableActions = _synchronizationService.SynchronizationProcessData.TotalActionsToProcess;
    }

    private void OnSynchronizationEnded(SynchronizationEnd synchronizationEnd)
    {
        EstimatedEndDateTimeLabel = Resources.SynchronizationMain_End;
        var synchronizationProgress = _synchronizationService.SynchronizationProcessData.SynchronizationProgress.Value;
        HandledActions = synchronizationProgress?.FinishedActionsCount ?? 0;
        Errors = synchronizationProgress?.ErrorActionsCount ?? 0;
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
