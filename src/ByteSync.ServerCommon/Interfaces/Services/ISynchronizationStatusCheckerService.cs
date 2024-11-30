using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISynchronizationStatusCheckerService
{
    bool CheckSynchronizationCanBeUpdated(SynchronizationEntity? synchronization);
    
    bool CheckSynchronizationCanBeAborted(SynchronizationEntity? synchronization);
}