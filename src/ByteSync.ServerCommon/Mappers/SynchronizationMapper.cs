using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Mappers;

namespace ByteSync.ServerCommon.Mappers;

public class SynchronizationMapper : ISynchronizationMapper
{
    public Synchronization MapToSynchronization(SynchronizationEntity synchronizationEntity)
    {
        Synchronization synchronization = new Synchronization();

        synchronization.SessionId = synchronizationEntity.SessionId;
        synchronization.Started = synchronizationEntity.StartedOn;
        synchronization.StartedBy = synchronizationEntity.StartedBy;
        synchronization.IsFatalError = synchronizationEntity.IsFatalError;
        synchronization.Ended = synchronizationEntity.EndedOn;
        synchronization.EndStatus = synchronizationEntity.EndStatus;
        synchronization.AbortRequestedOn = synchronizationEntity.AbortRequestedOn;
        synchronization.AbortRequestedBy = synchronizationEntity.AbortRequestedBy;
        
        return synchronization;
    }
}