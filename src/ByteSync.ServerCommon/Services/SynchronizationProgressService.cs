using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;

namespace ByteSync.ServerCommon.Services;

public class SynchronizationProgressService : ISynchronizationProgressService
{
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ITrackingActionMapper _trackingActionMapper;
    private readonly ISynchronizationMapper _synchronizationMapper;

    public SynchronizationProgressService(IInvokeClientsService invokeClientsService, ITrackingActionMapper trackingActionMapper, 
        ISynchronizationMapper synchronizationMapper)
    {
        _invokeClientsService = invokeClientsService;
        _trackingActionMapper = trackingActionMapper;
        _synchronizationMapper = synchronizationMapper;
    }

    public async Task UpdateSynchronizationProgress(TrackingActionResult trackingActionResult, bool needSendSynchronizationUpdated)
    {
        await SendSynchronizationProgressUpdated(trackingActionResult);
        
        if (needSendSynchronizationUpdated)
        {
            await SendSynchronizationUpdated(trackingActionResult.SynchronizationEntity);
        }
    }
    
    public async Task UpdateSynchronizationProgress(SynchronizationEntity synchronizationEntity, bool needSendSynchronizationUpdated)
    {
        await SendSynchronizationProgressUpdated(synchronizationEntity);
        
        if (needSendSynchronizationUpdated)
        {
            await SendSynchronizationUpdated(synchronizationEntity);
        }
    }

    public async Task InformSynchronizationStarted(SynchronizationEntity synchronizationEntity, Client client)
    {
        var synchronization = await MapToSynchronization(synchronizationEntity);
        await _invokeClientsService.SessionGroup(synchronization.SessionId).SynchronizationStarted(synchronization);
    }

    private Task<Synchronization> MapToSynchronization(SynchronizationEntity synchronizationEntity)
    {
        var synchronization = _synchronizationMapper.MapToSynchronization(synchronizationEntity);

        return Task.FromResult(synchronization);
    }

    private async Task SendSynchronizationUpdated(SynchronizationEntity synchronizationEntity)
    {
        var synchronization = await MapToSynchronization(synchronizationEntity);
        
        await _invokeClientsService.Clients(synchronizationEntity.Progress.Members).SynchronizationUpdated(synchronization);
    }

    private async Task SendSynchronizationProgressUpdated(SynchronizationEntity synchronizationEntity)
    {
        var synchronizationProgressPush = CreateSynchronizationProgressPush(synchronizationEntity, null);
        
        await _invokeClientsService.Clients(synchronizationEntity.Progress.Members)
            .SynchronizationProgressUpdated(synchronizationProgressPush);
    }

    private async Task SendSynchronizationProgressUpdated(TrackingActionResult trackingActionResult)
    {
        List<TrackingActionSummary> trackingActionSummaries = new List<TrackingActionSummary>();
        foreach (var trackingActionEntity in trackingActionResult.TrackingActionEntities)
        {
            var trackingActionSummary = _trackingActionMapper.MapToTrackingActionSummary(trackingActionEntity);
            
            trackingActionSummaries.Add(trackingActionSummary);
        }
        
        var synchronizationProgressPush = CreateSynchronizationProgressPush(trackingActionResult.SynchronizationEntity, trackingActionSummaries);
        
        await _invokeClientsService.Clients(trackingActionResult.SynchronizationEntity.Progress.Members)
            .SynchronizationProgressUpdated(synchronizationProgressPush);
    }

    private static SynchronizationProgressPush CreateSynchronizationProgressPush(SynchronizationEntity synchronizationEntity, 
        List<TrackingActionSummary>? trackingActionSummaries)
    {
        var synchronizationProgressPush = new SynchronizationProgressPush
        {
            SessionId = synchronizationEntity.SessionId,
            
            ProcessedVolume = synchronizationEntity.Progress.ProcessedVolume,
            ExchangedVolume = synchronizationEntity.Progress.ExchangedVolume,
            FinishedActionsCount = synchronizationEntity.Progress.FinishedAtomicActionsCount,
            ErrorActionsCount = synchronizationEntity.Progress.ErrorsCount,
            
            Version = DateTimeOffset.UtcNow.Ticks,
            
            TrackingActionSummaries = trackingActionSummaries,
        };
        return synchronizationProgressPush;
    }
}