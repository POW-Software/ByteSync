using System.Threading;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class SessionMemberApiClient : ISessionMemberApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<SessionMemberApiClient> _logger;
    
    public SessionMemberApiClient(IApiInvoker apiInvoker, ILogger<SessionMemberApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task<List<string>> GetMembersClientInstanceIds(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return (await _apiInvoker.GetAsync<List<string>>($"session/{sessionId}/members/InstanceIds", cancellationToken))!;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while getting session members client instance ids with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }
    
    public async Task<List<SessionMemberInfoDTO>> GetMembers(string sessionId)
    {
        try
        {
            return await _apiInvoker.GetAsync<List<SessionMemberInfoDTO>>($"session/{sessionId}/members");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while getting session members with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task UpdateSessionMemberGeneralStatus(UpdateSessionMemberGeneralStatusParameters sessionMemberGeneralStatusParameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionMemberGeneralStatusParameters.SessionId}/members/{sessionMemberGeneralStatusParameters.ClientInstanceId}/generalStatus", 
                sessionMemberGeneralStatusParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting local inventory status changed");
                
            throw;
        }
    }
}