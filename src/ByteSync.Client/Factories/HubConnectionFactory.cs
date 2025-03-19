using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Factories;

public class HubConnectionFactory : IHubConnectionFactory
{
    private readonly IConnectionConstantsService _connectionConstantsService;
    private readonly IAuthenticationTokensRepository _authenticationTokensRepository;
    private readonly ILogger<HubConnectionFactory> _logger;

    public HubConnectionFactory(IConnectionConstantsService connectionConstantsService, IAuthenticationTokensRepository authenticationTokensRepository,
        ILogger<HubConnectionFactory> logger)
    {
        _connectionConstantsService = connectionConstantsService;
        _authenticationTokensRepository = authenticationTokensRepository;
        _logger = logger;
    }
    
    public async Task<HubConnection> BuildConnection()
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
        
        ListenEvents(connection);

        await connection.StartAsync();

        return connection;
    }

    private void ListenEvents(HubConnection connection)
    {
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
    }
}