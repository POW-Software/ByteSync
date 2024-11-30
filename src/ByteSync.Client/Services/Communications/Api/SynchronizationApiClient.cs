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
    
    public async Task<Synchronization> StartSynchronization(SynchronizationStartRequest synchronizationStartRequest)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<Synchronization>($"session/{synchronizationStartRequest.SessionId}/synchronization/start", 
                synchronizationStartRequest);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting synchronization with sessionId: {sessionId}", synchronizationStartRequest.SessionId);
                
            throw;
        }
    }

    public async Task AssertLocalCopyIsDone(string sessionId, List<string> actionsGroupIds)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/localCopyIsDone", 
                actionsGroupIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that local copy is done: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task AssertDateIsCopied(string sessionId, List<string> actionsGroupIds)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/dateIsCopied", 
                actionsGroupIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that date is copied: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task AssertFileOrDirectoryIsDeleted(string sessionId, List<string> actionsGroupIds)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/fileOrDirectoryIsDeleted", actionsGroupIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that file or directory is deleted: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task AssertDirectoryIsCreated(string sessionId, List<string> actionsGroupIds)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/directoryIsCreated", actionsGroupIds);
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

    public async Task InformSynchronizationActionError(SharedFileDefinition sharedFileDefinition)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sharedFileDefinition.SessionId}/synchronization/error/", sharedFileDefinition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing synchronization action error: {sessionId}", sharedFileDefinition.SessionId);

            throw;
        }
    }

    public async Task AssertSynchronizationActionErrors(string sessionId, List<string> actionsGroupIds)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/synchronization/errors/", actionsGroupIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing synchronization action error: {sessionId}", sessionId);

            throw;
        }
    }
}