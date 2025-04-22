using ByteSync.Common.Business.SharedFiles;
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
    private readonly ISharedFilesService _sharedFilesService;

    public SynchronizationProgressService(IInvokeClientsService invokeClientsService, ITrackingActionMapper trackingActionMapper, 
        ISynchronizationMapper synchronizationMapper, ISharedFilesService sharedFilesService)
    {
        _invokeClientsService = invokeClientsService;
        _trackingActionMapper = trackingActionMapper;
        _synchronizationMapper = synchronizationMapper;
        _sharedFilesService = sharedFilesService;
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

    public async Task UploadIsFinished(SharedFileDefinition sharedFileDefinition, int totalParts, ICollection<string> targetInstanceIds)
    {
        targetInstanceIds = new HashSet<string>(targetInstanceIds);
        
        await _sharedFilesService.AssertUploadIsFinished(sharedFileDefinition, totalParts, targetInstanceIds);

        var fileTransferPush = new FileTransferPush
        {
            SessionId = sharedFileDefinition.SessionId,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = totalParts,
            ActionsGroupIds = sharedFileDefinition.ActionsGroupIds!
        };
        
        await _invokeClientsService.Clients(targetInstanceIds).UploadFinished(fileTransferPush);
    }

    public async Task FilePartIsUploaded(SharedFileDefinition sharedFileDefinition, int partNumber, HashSet<string> targetInstanceIds)
    {
        await _sharedFilesService.AssertFilePartIsUploaded(sharedFileDefinition, partNumber, targetInstanceIds);
            
        var fileTransferPush = new FileTransferPush
        {
            SessionId = sharedFileDefinition.SessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber,
            ActionsGroupIds = sharedFileDefinition.ActionsGroupIds!
        };
        
        await _invokeClientsService.Clients(targetInstanceIds).FilePartUploaded(fileTransferPush);
    }

    public Task<Synchronization> MapToSynchronization(SynchronizationEntity synchronizationEntity)
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
            FinishedActionsCount = synchronizationEntity.Progress.FinishedActionsCount,
            ErrorActionsCount = synchronizationEntity.Progress.ErrorsCount,
            
            Version = DateTimeOffset.Now.Ticks,
            
            TrackingActionSummaries = trackingActionSummaries,
        };
        return synchronizationProgressPush;
    }
}