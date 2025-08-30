using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ISynchronizationApiClient
{
    public Task StartSynchronization(SynchronizationStartRequest synchronizationStartRequest);
    
    public Task AssertLocalCopyIsDone(string sessionId, SynchronizationActionRequest synchronizationActionRequest);

    public Task AssertDateIsCopied(string sessionId, SynchronizationActionRequest synchronizationActionRequest);

    public Task AssertFileOrDirectoryIsDeleted(string sessionId, SynchronizationActionRequest synchronizationActionRequest);

    public Task AssertDirectoryIsCreated(string sessionId, SynchronizationActionRequest synchronizationActionRequest);

    public Task RequestAbortSynchronization(string sessionId);
    
    Task InformCurrentMemberHasFinishedSynchronization(CloudSession cloudSession);
    
    Task InformSynchronizationActionError(SharedFileDefinition sharedFileDefinition, string? nodeId);
    
    Task AssertSynchronizationActionErrors(string sessionId, SynchronizationActionRequest synchronizationActionRequest);
}