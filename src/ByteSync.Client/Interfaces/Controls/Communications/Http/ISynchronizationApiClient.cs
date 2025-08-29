using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ISynchronizationApiClient
{
    public Task StartSynchronization(SynchronizationStartRequest synchronizationStartRequest);
    
    public Task AssertLocalCopyIsDone(string sessionId, List<string> actionsGroupIds, string? nodeId);

    public Task AssertDateIsCopied(string sessionId, List<string> actionsGroupIds, string? nodeId);

    public Task AssertFileOrDirectoryIsDeleted(string sessionId, List<string> actionsGroupIds, string? nodeId);

    public Task AssertDirectoryIsCreated(string sessionId, List<string> actionsGroupIds, string? nodeId);

    public Task RequestAbortSynchronization(string sessionId);
    
    Task InformCurrentMemberHasFinishedSynchronization(CloudSession cloudSession);
    
    Task InformSynchronizationActionError(SharedFileDefinition sharedFileDefinition, string? nodeId);
    
    Task AssertSynchronizationActionErrors(string sessionId, List<string> actionsGroupIds, string? nodeId);
}