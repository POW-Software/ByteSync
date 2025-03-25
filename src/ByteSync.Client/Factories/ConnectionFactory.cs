using ByteSync.Business.Communications;
using ByteSync.Common.Business.Auth;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Misc;

namespace ByteSync.Factories;

public class ConnectionFactory : IConnectionFactory
{
    private readonly IAuthenticationTokensRepository _authenticationTokensRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly IAuthApiClient _authApiClient;
    private readonly IHubConnectionFactory _hubConnectionFactory;
    private readonly ILogger<ConnectionFactory> _logger;

    public ConnectionFactory(IAuthenticationTokensRepository authenticationTokensRepository, IEnvironmentService environmentService,
        IAuthApiClient authApiClient, IHubConnectionFactory hubConnectionFactory, ILogger<ConnectionFactory> logger)
    {
        _authenticationTokensRepository = authenticationTokensRepository;
        _environmentService = environmentService;
        _authApiClient = authApiClient;
        _hubConnectionFactory = hubConnectionFactory;
        _logger = logger;
    }
    
    public async Task<BuildConnectionResult> BuildConnection()
    {
        _logger.LogInformation("Starting login with the server");

        var authenticationResponse = await GetInitialAuthenticationTokens();
        if (authenticationResponse is not { IsSuccess: true })
        {
            _logger.LogError("Login with the server failed. Details: {details}", authenticationResponse?.InitialConnectionStatus);
            return new BuildConnectionResult
            {
                AuthenticateResponseStatus = authenticationResponse?.InitialConnectionStatus
            };
        }

        var hubConnection = await _hubConnectionFactory.BuildConnection();
        
        var result = new BuildConnectionResult
        {
            HubConnection = hubConnection,
            EndPoint = authenticationResponse.EndPoint
        };
        
        _logger.LogDebug("ConnectionId:{connectionId}", hubConnection.ConnectionId);

        return result;
    }

    public async Task<bool> RefreshAuthenticationTokens()
    {
        var tokens = (await _authenticationTokensRepository.GetTokens())!;
        
        if (tokens.RefreshTokenExpiration < DateTimeOffset.Now)
        {
            _logger.LogWarning("Cannot refresh tokens: refresh token is expired");
            return false;
        }
        
        var refreshTokensData = new RefreshTokensData
        {
            ClientInstanceId = _environmentService.ClientInstanceId,
            OsPlatform = _environmentService.OSPlatform,
            Version = VersionHelper.GetVersionString(_environmentService.ApplicationVersion),
            Token = tokens.RefreshToken
        };
        
        var refreshTokensResponse = await _authApiClient.RefreshAuthenticationTokens(refreshTokensData);
        var isSuccess = refreshTokensResponse is { IsSuccess: true };
        
        if (isSuccess)
        {
            await _authenticationTokensRepository.Store(refreshTokensResponse!.AuthenticationTokens!);
        }

        return isSuccess;
    }
    
    private async Task<InitialAuthenticationResponse?> GetInitialAuthenticationTokens()
    {
        var loginData = new LoginData
        {
            ClientId =  _environmentService.ClientId,
            ClientInstanceId = _environmentService.ClientInstanceId,
            OsPlatform = _environmentService.OSPlatform,
            Version = VersionHelper.GetVersionString(_environmentService.ApplicationVersion),
        };
        
        var authenticationResponse = await _authApiClient.Login(loginData);
        
        if (authenticationResponse is { IsSuccess: true })
        {
            await _authenticationTokensRepository.Store(authenticationResponse.AuthenticationTokens!);
        }

        return authenticationResponse;
    }
}