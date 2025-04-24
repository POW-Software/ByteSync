using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISynchronizationService
{
    Task StartSynchronization(string sessionId, Client client, List<ActionsGroupDefinition> actionsGroupDefinitions);
    
    Task OnUploadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, int totalParts, Client client);
    
    Task OnFilePartIsUploadedAsync(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task OnDownloadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, Client client);
    
    Task OnDateIsCopied(string sessionId, List<string> actionsGroupIds, Client client);
    
    Task OnFileOrDirectoryIsDeletedAsync(string sessionId, List<string> actionsGroupIds, Client client);
    
    Task OnDirectoryIsCreatedAsync(string sessionId, List<string> actionsGroupIds, Client client);
    
    Task RequestAbortSynchronization(string sessionId, Client client);
    
    Task<Synchronization?> GetSynchronization(string sessionId, Client client);

    Task OnLocalCopyIsDoneAsync(string sessionId, List<string> actionsGroupIds, Client client);
    
    Task AssertSynchronizationActionErrors(string sessionId, List<string> actionsGroupIds, Client client);

    Task OnMemberHasFinished(string sessionId, Client client);
    
    Task ResetSession(string sessionId);
}