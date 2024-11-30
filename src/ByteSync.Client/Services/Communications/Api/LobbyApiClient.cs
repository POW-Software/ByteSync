using System.Threading.Tasks;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class LobbyApiClient : ILobbyApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<LobbyApiClient> _logger;
    
    public LobbyApiClient(IApiInvoker apiInvoker, ILogger<LobbyApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials credentials)
    {
        try
        {
            await _apiInvoker.PostAsync($"lobby/{credentials.LobbyId}/sendCloudSessionCredentials", credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
                
            throw;
        }
    }

    public async Task<JoinLobbyResult> JoinLobby(JoinLobbyParameters joinLobbyParameters)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<JoinLobbyResult>($"lobby/join/{joinLobbyParameters.CloudSessionProfileId}", joinLobbyParameters);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
                
            throw;
        }
    }

    public async Task QuitLobby(string lobbyId)
    {
        try
        {
            await _apiInvoker.PostAsync($"lobby/{lobbyId}/quit");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
                
            throw;
        }
    }

    public async Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo)
    {
        try
        {
            await _apiInvoker.PostAsync($"lobby/{lobbyCheckInfo.LobbyId}/checkInfos", lobbyCheckInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
                
            throw;
        }
    }

    public async Task UpdateLobbyMemberStatus(string lobbyId, LobbyMemberStatuses status)
    {
        try
        {
            await _apiInvoker.PostAsync($"lobby/{lobbyId}/memberStatus", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
                
            throw;
        }
    }
}