using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISynchronizationService
{
    Task OnDownloadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, Client client);
    
    Task ResetSession(string sessionId);
    
    bool CheckSynchronizationIsFinished(SynchronizationEntity synchronizationEntity);
}