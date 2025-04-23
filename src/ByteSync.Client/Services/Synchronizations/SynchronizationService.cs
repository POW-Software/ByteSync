using System.Reactive.Linq;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationService : ISynchronizationService
{
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ISynchronizationLooperFactory _synchronizationLooperFactory;
    private readonly ITimeTrackingCache _timeTrackingCache;
    private readonly ILogger<SynchronizationService> _logger;

    public SynchronizationService(ISessionService sessionService, ISessionMemberService sessionMemberService, ISynchronizationApiClient synchronizationApiClient, 
        ISynchronizationLooperFactory synchronizationLooperFactory, ITimeTrackingCache timeTrackingCache, ILogger<SynchronizationService> logger)
    {
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _synchronizationApiClient = synchronizationApiClient;
        _synchronizationLooperFactory = synchronizationLooperFactory;
        _timeTrackingCache = timeTrackingCache;
        _logger = logger;

        
        SynchronizationProcessData = new SynchronizationProcessData();
        
        SynchronizationProcessData.SynchronizationStart.CombineLatest(SynchronizationProcessData.SynchronizationDataTransmitted)
            .Where(tuple => tuple is { First: not null, Second: true })
            .Subscribe(_ =>
            {
                Task.Run(async () =>
                {
                    var synchronizationLooper = _synchronizationLooperFactory.CreateSynchronizationLooper();
                    await synchronizationLooper.CloudSessionSynchronizationLoop();
                    await synchronizationLooper.DisposeAsync();
                });
            });
        
        _sessionService.SessionStatusObservable
            .Where(x => x == SessionStatus.Preparation)
            .Subscribe(_ => SynchronizationProcessData.Reset());
    }

    public SynchronizationProcessData SynchronizationProcessData { get; }
    
    public async Task AbortSynchronization()
    {
        var session = await _sessionService.SessionObservable.FirstOrDefaultAsync();
        
        if (session is CloudSession cloudSession)
        {
            await _synchronizationApiClient.RequestAbortSynchronization(cloudSession.SessionId);
        }
        else
        {
            _logger.LogError("CloudSessionManager.AbortSynchronization: unknown session");
        }
    }

    public async Task OnSynchronizationUpdated(Synchronization synchronization)
    {
        if (synchronization.IsEnded)
        {
            var timeTrackingComputer = await _timeTrackingCache
                .GetTimeTrackingComputer(_sessionService.SessionId!, TimeTrackingComputerType.Synchronization);
            timeTrackingComputer.Stop();
            
            var synchronizationEnd = new SynchronizationEnd
            {
                SessionId = synchronization.SessionId,
                FinishedOn = synchronization.Ended!.Value,
                Status = synchronization.EndStatus!.Value,
            };
        
            SynchronizationProcessData.SynchronizationEnd.OnNext(synchronizationEnd);
            
            _ = _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationFinished);
        }

        if (synchronization.IsAbortRequested)
        {
            var sar = new SynchronizationAbortRequest
            {
                SessionId = synchronization.SessionId,
                RequestedOn = synchronization.AbortRequestedOn!.Value,
                RequestedBy = synchronization.AbortRequestedBy,
            };
            
            SynchronizationProcessData.SynchronizationAbortRequest.OnNext(sar);
        }
    }

    public async Task OnSynchronizationStarted(Synchronization synchronization)
    {
        try
        {
            await _sessionService.SetSessionStatus(SessionStatus.Synchronization);
            SynchronizationProcessData.SynchronizationMainStatus.OnNext(SynchronizationProcessStatuses.Running);
            
            var timeTrackingComputer = await _timeTrackingCache
                .GetTimeTrackingComputer(_sessionService.SessionId!, TimeTrackingComputerType.Synchronization);
            timeTrackingComputer.Start(synchronization.Started);
            
            SynchronizationProcessData.SynchronizationStart.OnNext(synchronization);

            _ = _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationRunning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnSynchronizationStarted");
        }
    }

    public Task OnSynchronizationDataTransmitted(SharedSynchronizationStartData sharedSynchronizationStartData)
    {
        SynchronizationProcessData.TotalVolumeToProcess = sharedSynchronizationStartData.TotalVolumeToProcess;
        SynchronizationProcessData.TotalActionsToProcess = sharedSynchronizationStartData.TotalActionsToProcess;
        
        SynchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        
        return Task.CompletedTask;
    }

    public Task OnSynchronizationProgressChanged(SynchronizationProgressPush synchronizationProgressPush)
    {
        var synchronizationProgress = SynchronizationProcessData.SynchronizationProgress.Value ?? new SynchronizationProgress();
        
        if (synchronizationProgressPush.Version > synchronizationProgress.Version)
        {
            synchronizationProgress.ExchangedVolume = synchronizationProgressPush.ExchangedVolume;
            synchronizationProgress.ProcessedVolume = synchronizationProgressPush.ProcessedVolume;
            synchronizationProgress.TotalVolumeToProcess = SynchronizationProcessData.TotalVolumeToProcess;
            synchronizationProgress.FinishedActionsCount = synchronizationProgressPush.FinishedActionsCount;
            synchronizationProgress.ErrorActionsCount = synchronizationProgressPush.ErrorActionsCount;
            synchronizationProgress.Version = synchronizationProgressPush.Version;
            
            SynchronizationProcessData.SynchronizationProgress.OnNext(synchronizationProgress);
        }

        return Task.CompletedTask;
    }
}