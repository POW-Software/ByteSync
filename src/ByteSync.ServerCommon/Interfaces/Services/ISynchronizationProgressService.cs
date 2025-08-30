using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISynchronizationProgressService
{
    Task UpdateSynchronizationProgress(TrackingActionResult actionsGroupIds, bool needSendSynchronizationUpdated);

    Task UpdateSynchronizationProgress(SynchronizationEntity synchronizationEntity, bool needSendSynchronizationUpdated);
    
    Task InformSynchronizationStarted(SynchronizationEntity synchronizationEntity, Client client);
    
    // FilePartIsUploaded inlined into AssertFilePartIsUploadedCommandHandler
}