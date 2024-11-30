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


        
        // var currentEndPoint = JsonConvert.DeserializeObject<ByteSyncEndpoint>(authenticationResponse.EndPoint);
        // SetCurrentMachineComparisonEndpoint(authenticationResponse.EndPoint!);

        // todo 200923
        // HubWrapper = new HubConnectionWrapper(_hubConnection);
        // HttpWrapper = new HttpConnectionWrapper(TokensStorer, ConstantsProvider);
        // HubPushHandler2.SetConnection(_hubConnection);

        // todo 200923
        // var currentEndPoint = JsonConvert.DeserializeObject<ByteSyncEndpoint>(authenticationResponse.EndPoint);
        // SetCurrentMachineComparisonEndpoint(authenticationResponse.EndPoint!);
        //
        // await Scheduler.Start();
        // await StartSchedule(ConstantsProvider.RegularJobKey, TokensStorer.RegularRefreshTokenTimeSpan, () => RefreshTokens(true));
        
        _logger.LogDebug("ConnectionId:{connectionId}", hubConnection.ConnectionId);
            
        // todo 220523
        // _navigationEventsHub.RaiseLogInSucceeded(currentEndPoint, authenticateResponse.ProductSerialDescription);

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
            Machinename = _environmentService.MachineName ?? "",
            OsPlatform = _environmentService.OSPlatform,
            Version = VersionHelper.GetVersionString(_environmentService.CurrentVersion),
            Token = tokens.RefreshToken
        };
        
        var refreshTokensResponse = await _authApiClient.RefreshAuthenticationTokens(refreshTokensData);
        var isSuccess = refreshTokensResponse is { IsSuccess: true };
        
        if (isSuccess)
        {
            await _authenticationTokensRepository.Store(refreshTokensResponse!.AuthenticationTokens!);
        }

        return isSuccess;
        
        
        
        
        
        
        
        
        
        
        
        


        // if (DateTimeOffset.Now - _authenticationTokensRepository.LastRefreshDateTime.GetValueOrDefault() < TimeSpan.FromSeconds(15))
        // {
        //     Log.Debug("ConnectionManager.RefreshTokens: previous refresh too recent");
        //
        //     // Le dernier refresh est trop récent
        //     if (scheduleAfter)
        //     {
        //         await StartSchedule(ConstantsProvider.RegularJobKey, ConstantsProvider.OnProblemRefreshTokensTimeSpan, () => RefreshTokens(true));
        //     }
        //
        //     return false;
        // }

        // await RefreshTokens();
        //
        // try
        // {
        //     // other Enconding  https://stackoverflow.com/questions/533527/how-do-i-replace-special-characters-in-a-url
        //
        //     string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //
        //     Uri uri = new Uri(ConstantsProvider.Host +
        //                       $"auth/refresh-token?refreshToken={System.Net.WebUtility.UrlEncode(TokensStorer.GetRefreshToken())}&" +
        //                       $"machineName={System.Net.WebUtility.UrlEncode(_localApplicationDataManager.MachineName)}&" +
        //                       $"clientInstanceId={System.Net.WebUtility.UrlEncode(ClientInstanceId)}&" +
        //                       $"osPlatform={_localApplicationDataManager.OSPlatform}&" +
        //                       $"version={System.Net.WebUtility.UrlEncode(version)}");
        //
        //     var httpClient = new HttpClient();
        //     httpClient.Timeout = TimeSpan.FromSeconds(20);
        //     var httpResponse = await httpClient.GetAsync(uri);
        //     httpResponse.EnsureSuccessStatusCode();
        //     var rawResponse = await httpResponse.Content.ReadAsStringAsync();
        //
        //     var authenticationResponse = JsonConvert.DeserializeObject<RefreshTokensResponse>(rawResponse);
        //
        //     if (authenticationResponse.IsSuccess)
        //     {
        //         Log.Debug("ConnectionManager.RefreshTokens: authenticateResponse.IsSuccess OK");
        //
        //         TokensStorer.Store(authenticationResponse.AuthenticationTokens!);
        //
        //         if (scheduleAfter)
        //         {
        //             await StartSchedule(ConstantsProvider.RegularJobKey, TokensStorer.RegularRefreshTokenTimeSpan, () => RefreshTokens(true));
        //         }
        //
        //
        //         return true;
        //
        //         //ITrigger trigger = CreateStandardTrigger();
        //
        //         //await context.Scheduler.RescheduleJob(new TriggerKey("getJwtTokenTrigger", "group1"), trigger);
        //     }
        //     else
        //     {
        //         //await context.Scheduler.Shutdown(false);
        //
        //         Log.Warning("ConnectionManager.RefreshTokens: !authenticateResponse.IsSuccess NOT OK");
        //     }
        // }
        // catch (Exception ex)
        // {
        //     ////context.Trigger.
        //
        //     //var oldTrigger = await context.Scheduler.GetTrigger(new TriggerKey("getJwtTokenTrigger", "group1"));
        //
        //     //if (oldTrigger != null)
        //     //{
        //     //    // obtain a builder that would produce the trigger
        //     //    TriggerBuilder tb = oldTrigger.GetTriggerBuilder();
        //
        //
        //     //}
        //
        //     // update the schedule associated with the builder, and build the new trigger
        //     // (other builder methods could be called, to change the trigger in any desired way)
        //     //var newTrigger = ConnectionManager.CreateErrorTrigger();
        //
        //     if (ex is HttpRequestException hre && hre.Message.Contains("401"))
        //     {
        //         //ex.ex
        //         // on tombe ici en cas d'erreur 401
        //
        //         Log.Error(hre, "ConnectionManager.RefreshTokens: Error 401, unable to refresh tokens, LogOut");
        //         // todo 220523
        //         // _navigationEventsHub.RaiseLogOutRequested();
        //             
        //         // _eventAggregator.GetEvent<LogOutRequested>().Publish();
        //     }
        //     else
        //     {
        //         Log.Error(ex, "ConnectionManager.RefreshTokens: Error, scheduleAfter:{ScheduleAfter}", scheduleAfter);
        //
        //         //await context.Scheduler.RescheduleJob(new TriggerKey("getJwtTokenTrigger", "group1"), newTrigger);
        //         if (scheduleAfter)
        //         {
        //             await StartSchedule(ConstantsProvider.RegularJobKey, ConstantsProvider.OnProblemRefreshTokensTimeSpan, () => RefreshTokens(true));
        //         }
        //     }
        // }
        //
        // return false;
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

        // todo 200923
        // HubConnectionPushHandler.HandleConnection(connection);

        // todo 200923
        // connection.Closed += ConnectionClosed;
        // connection.Reconnected += ConnectionReconnected;
        // connection.Reconnecting += ConnectionReconnecting;

        connection.Closed += exception =>
        {
            _logger.LogWarning("connection closed");
            return Task.CompletedTask;
        };
        
        connection.Reconnected += connectionId =>
        {
            _logger.LogWarning("connection reconnected");
            return Task.CompletedTask;
        };
        
        connection.Reconnecting += exception =>
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
            Version = VersionHelper.GetVersionString(_environmentService.CurrentVersion),
        };
        
        var authenticationResponse = await _authApiClient.Login(loginData);
        
        if (authenticationResponse is { IsSuccess: true })
        {
            await _authenticationTokensRepository.Store(authenticationResponse.AuthenticationTokens!);
        }

        return authenticationResponse;
    }
    
    // private async Task<RefreshTokensResponse> RefreshTokens()
    // {
    //     var refreshTokensData = new RefreshTokensData
    //     {
    //         ClientId = _applicationSettingsRepository.GetCurrentApplicationSettings().ClientId,
    //         ClientInstanceId = _localApplicationDataManager.ClientInstanceId,
    //         Machinename = _localApplicationDataManager.MachineName ?? "",
    //         OsPlatform = _localApplicationDataManager.OSPlatform,
    //         Version = GetVersion(),
    //         Token = _authenticationTokensRepository.GetRefreshToken()
    //     };
    //     
    //     RefreshTokensResponse refreshTokensResponse = await _authApiClient.RefreshAuthenticationTokens(refreshTokensData);
    //     
    //     if (refreshTokensResponse.IsSuccess)
    //     {
    //         _authenticationTokensRepository.Store(refreshTokensResponse.AuthenticationTokens!);
    //     }
    //
    //     return refreshTokensResponse;
    // }

    // private static string GetVersion()
    // {
    //     string version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
    //     
    //     return version;
    // }
}