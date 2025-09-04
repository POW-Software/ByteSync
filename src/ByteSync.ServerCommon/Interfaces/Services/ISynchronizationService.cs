using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISynchronizationService
{
    Task ResetSession(string sessionId);
    
    bool CheckSynchronizationIsFinished(SynchronizationEntity synchronizationEntity);
}