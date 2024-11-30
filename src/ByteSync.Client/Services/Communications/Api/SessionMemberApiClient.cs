using System.Threading.Tasks;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
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

    public async Task UpdateSessionMemberGeneralStatus(UpdateSessionMemberGeneralStatusParameters sessionMemberGeneralStatusParameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionMemberGeneralStatusParameters.SessionId}/inventory/localStatus", 
                sessionMemberGeneralStatusParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting local inventory status changed");
                
            throw;
        }
    }
}