using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Services;

public class SynchronizationService : ISynchronizationService
{
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;

    public SynchronizationService(ISynchronizationRepository synchronizationRepository,
        ITrackingActionRepository trackingActionRepository, ISynchronizationProgressService synchronizationProgressService, 
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService)
    {
        _synchronizationRepository = synchronizationRepository;
        _trackingActionRepository = trackingActionRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
    }
    
    // OnDownloadIsFinishedAsync has been inlined into AssertDownloadIsFinishedCommandHandler

    public bool CheckSynchronizationIsFinished(SynchronizationEntity synchronizationEntity)
    {
        bool isUpdated = false;
        
        if (!synchronizationEntity.IsEnded && 
            (synchronizationEntity.Progress.AllMembersCompleted && 
                (synchronizationEntity.Progress.AllActionsDone || synchronizationEntity.IsAbortRequested)))
        {
            synchronizationEntity.EndedOn = DateTimeOffset.UtcNow;
            
            if (synchronizationEntity.IsAbortRequested)
            {
                synchronizationEntity.EndStatus = SynchronizationEndStatuses.Abortion;
            }
            else
            {
                synchronizationEntity.EndStatus = SynchronizationEndStatuses.Regular;
            }
            
            isUpdated = true;
        }

        return isUpdated;
    }

    public async Task ResetSession(string sessionId)
    {
        await _synchronizationRepository.ResetSession(sessionId);
    }

}