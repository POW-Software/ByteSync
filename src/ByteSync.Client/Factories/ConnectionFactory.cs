using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Misc;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Factories;

public class ConnectionFactory : IConnectionFactory
{
    private readonly IAuthenticationTokensRepository _authenticationTokensRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly IConnectionConstantsService _connectionConstantsService;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IAuthApiClient _authApiClient;
    private readonly ILogger<ConnectionFactory> _logger;

    public ConnectionFactory(IAuthenticationTokensRepository authenticationTokensRepository, IEnvironmentService environmentService,
        IConnectionConstantsService connectionConstantsService, IApplicationSettingsRepository applicationSettingsRepository, 
        IAuthApiClient authApiClient, ILogger<ConnectionFactory> logger)
    {
        _authenticationTokensRepository = authenticationTokensRepository;
        _environmentService = environmentService;
        _connectionConstantsService = connectionConstantsService;
        _applicationSettingsRepository = applicationSettingsRepository;
        _authApiClient = authApiClient;
        _logger = logger;

    }
    
    public async Task<BuildConnectionResult> BuildConnection()
    {
        _logger.LogInformation("Starting login with the server");

        var authenticationResponse = await GetInitialAuthenticationTokens();
        if (authenticationResponse is not { IsSuccess: true })
        {
            _logger.LogError("Login with the server failed. Details: {details}", authenticationResponse?.InitialConnectionStatus);
            return new BuildConnectionResult();
        }

        var hubConnection = await StartConnection();
        
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

    private async Task<HubConnection> StartConnection()
    {
        var apiUrl = await _connectionConstantsService.GetApiUrl();
        var url = UrlUtils.AppendSegment(apiUrl, "auth");
        
        var tokens = (await _authenticationTokensRepository.GetTokens())!;
        
        var connectionBuilder =

            new HubConnectionBuilder()
                .WithUrl(url,
                    options =>
                    {
                        options.Headers.Add("Authorization", tokens.JwtToken);
                        options.Transports = HttpTransportType.WebSockets;
                    })
            #if DEBUG
                .ConfigureLogging(logging =>
                {
                    // This will set ALL logging to Debug level
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                })
            #endif
                .WithAutomaticReconnect(_connectionConstantsService.GetRetriesTimeSpans());

        var connection = connectionBuilder.Build();

        connection.Closed += _ =>
        {
            _logger.LogWarning("connection closed");
            return Task.CompletedTask;
        };
        
        connection.Reconnected += _ =>
        {
            _logger.LogWarning("connection reconnected");
            return Task.CompletedTask;
        };
        
        connection.Reconnecting += _ =>
        {
            _logger.LogWarning("connection reconnecting");
            return Task.CompletedTask;
        };

        await connection.StartAsync();

        return connection;
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