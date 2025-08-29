using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class SynchronizationApiClient : ISynchronizationApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<SynchronizationApiClient> _logger;

    public SynchronizationApiClient(IApiInvoker apiInvoker, ILogger<SynchronizationApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task StartSynchronization(SynchronizationStartRequest synchronizationStartRequest)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{synchronizationStartRequest.SessionId}/synchronization/start", 
                synchronizationStartRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting synchronization with sessionId: {sessionId}", synchronizationStartRequest.SessionId);
                
            throw;
        }
    }

    public async Task AssertLocalCopyIsDone(string sessionId, List<string> actionsGroupIds, string? nodeId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/localCopyIsDone", 
                new SynchronizationActionRequest(actionsGroupIds, nodeId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that local copy is done: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task AssertDateIsCopied(string sessionId, List<string> actionsGroupIds, string? nodeId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/dateIsCopied", 
                new SynchronizationActionRequest(actionsGroupIds, nodeId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that date is copied: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task AssertFileOrDirectoryIsDeleted(string sessionId, List<string> actionsGroupIds, string? nodeId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/fileOrDirectoryIsDeleted", 
                new SynchronizationActionRequest(actionsGroupIds, nodeId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that file or directory is deleted: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task AssertDirectoryIsCreated(string sessionId, List<string> actionsGroupIds, string? nodeId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/directoryIsCreated", 
                new SynchronizationActionRequest(actionsGroupIds, nodeId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that directory is created: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task RequestAbortSynchronization(string sessionId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/abort");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while requesting abort synchronization: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task InformCurrentMemberHasFinishedSynchronization(CloudSession cloudSession)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{cloudSession.SessionId}/synchronization/memberHasFinished");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that synchronization is finished: {sessionId}", cloudSession.SessionId);

            throw;
        }
    }

    public async Task InformSynchronizationActionError(SharedFileDefinition sharedFileDefinition, string? nodeId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sharedFileDefinition.SessionId}/synchronization/error/", 
                new SynchronizationErrorActionRequest(sharedFileDefinition, nodeId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing synchronization action error: {sessionId}", sharedFileDefinition.SessionId);

            throw;
        }
    }

    public async Task AssertSynchronizationActionErrors(string sessionId, List<string> actionsGroupIds, string? nodeId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/errors/", 
                new SynchronizationActionRequest(actionsGroupIds, nodeId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing synchronization action error: {sessionId}", sessionId);

            throw;
        }
    }
}