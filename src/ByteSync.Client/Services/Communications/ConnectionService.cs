using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Exceptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using Microsoft.AspNetCore.SignalR.Client;
using Polly;

namespace ByteSync.Services.Communications;

public class ConnectionService : IConnectionService, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IAuthenticationTokensRepository _authenticationTokensRepository;
    private readonly ILogger<ConnectionService> _logger;
    
    private readonly IDisposable? _connectionSubscription;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    public ConnectionService(IConnectionFactory connectionFactory, IAuthenticationTokensRepository authenticationTokensRepository, ILogger<ConnectionService> logger)
    {
        _connectionFactory = connectionFactory;
        _authenticationTokensRepository = authenticationTokensRepository;
        _logger = logger;
        
        ConnectionSubject = new BehaviorSubject<HubConnection?>(null);
        ConnectionStatusSubject = new BehaviorSubject<ConnectionStatuses>(ConnectionStatuses.NotConnected);
        
        _connectionSubscription = Connection
            .Where(connection => connection != null)
            .SelectMany(async connection =>
            {
                await StartOrRestartJwtTokensRefreshTimer();
                return connection;
            })
            .Subscribe(connection =>
            {
                connection!.Closed += (error) =>
                {
                    _logger.LogError("Connection closed: {Error}", error?.Message ?? "Unknown error");
                    
                    _refreshCancellationTokenSource?.Cancel();
                    
                    return Task.CompletedTask;
                };
            });
    }
    
    private BehaviorSubject<ConnectionStatuses> ConnectionStatusSubject { get; set; }
    
    private BehaviorSubject<HubConnection?> ConnectionSubject { get; set; }
    
    public IObservable<ConnectionStatuses> ConnectionStatus => ConnectionStatusSubject.AsObservable();
    
    public ConnectionStatuses CurrentConnectionStatus => ConnectionStatusSubject.Value;

    public IObservable<HubConnection?> Connection => ConnectionSubject.AsObservable();
    
    public ByteSyncEndpoint? CurrentEndPoint { get; set; }
    
    public string? ClientInstanceId => CurrentEndPoint?.ClientInstanceId;
    
    public async Task StartConnectionAsync()
    {
        var retryPolicy = Policy
            .Handle<Exception>(ex => !(ex is BuildConnectionException bce && bce.InitialConnectionStatus == InitialConnectionStatus.VersionNotAllowed))
            .WaitAndRetryForeverAsync(
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, _, _) =>
                {
                    ConnectionStatusSubject.OnNext(ConnectionStatuses.NotConnected);
                    ConnectionSubject.OnNext(null);

                    _logger.LogError(exception, "An error occurred while starting the connection");
                });
        
        await retryPolicy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Starting connection");

            ConnectionStatusSubject.OnNext(ConnectionStatuses.Connecting);

            var result = await _connectionFactory.BuildConnection();
            
            ConnectionSubject.OnNext(result.HubConnection);
            
            if (result.HubConnection != null)
            {
                CurrentEndPoint = result.EndPoint!;
                
                ConnectionStatusSubject.OnNext(ConnectionStatuses.Connected);
            }
            else
            {
                if (result.AuthenticateResponseStatus == InitialConnectionStatus.VersionNotAllowed)
                {
                    ConnectionStatusSubject.OnNext(ConnectionStatuses.UpdateNeeded);
                }
                
                throw new BuildConnectionException("Unable to connect", result.AuthenticateResponseStatus);
            }
        });
    }
    
    private async Task StartOrRestartJwtTokensRefreshTimer()
    {
        await CancelCurrentRefreshCancellationTokenSource();

        _refreshCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _refreshCancellationTokenSource.Token;

        try
        {
            var tokens = await _authenticationTokensRepository.GetTokens();
            if (tokens != null)
            {
                var periodSeconds = tokens.JwtTokenDurationInSeconds / 2 - 1;
                var period = TimeSpan.FromSeconds(periodSeconds);
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(period, cancellationToken);
                    
                    try
                    {
                        await RefreshJwtTokens();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while refreshing JWT tokens");
                        await RestartConnection();
                    }
                }
            }
            else
            {
                _logger.LogError("An unexpected error occurred in the JWT tokens refresh loop");
            }
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in the JWT tokens refresh loop");
        }
    }

    private async Task RefreshJwtTokens()
    {
        var retryPolicy = Policy
            .Handle<Exception>(ex => !(ex is HttpRequestException httpRequestException && httpRequestException.Message.Contains("401")))
            .Or<HttpRequestException>(ex => !ex.Message.Contains("401"))
            .WaitAndRetryAsync(5,
                _ => TimeSpan.FromSeconds(10),
                (exception, _, currentAttempt, _) =>
                {
                    _logger.LogError(exception, "Attempt {CurrentAttempt}: An error occurred while refreshing jwt tokens", currentAttempt);
                });
        
        await retryPolicy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Start refreshing jwt tokens");

            var refreshTokens = await _connectionFactory.RefreshAuthenticationTokens();
            if (refreshTokens)
            {
                _logger.LogInformation("successfully refreshed jwt tokens");
            }
            else
            {
                _logger.LogWarning("failed to refresh jwt tokens");

                _ = RestartConnection();
            }
        });
    }

    public async Task StopConnection()
    {
        await CancelCurrentRefreshCancellationTokenSource();
        
        var connection = ConnectionSubject.Value;
        if (connection != null)
        {
            try
            {
                await connection.StopAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while stopping the connection");
            }
        }

        ConnectionSubject.OnNext(null);
        ConnectionStatusSubject.OnNext(ConnectionStatuses.NotConnected);
    }
    
    public void Dispose()
    {
        _connectionSubscription?.Dispose();
        _refreshCancellationTokenSource?.Dispose();
    }
    
    private async Task RestartConnection()
    {
        await StopConnection();
        await StartConnectionAsync();
    }
    
    private Task CancelCurrentRefreshCancellationTokenSource()
    {
        var currentRefreshCancellationTokenSource = _refreshCancellationTokenSource;
        if (currentRefreshCancellationTokenSource != null)
        {
            _ = currentRefreshCancellationTokenSource.CancelAsync();
        }
        
        return Task.CompletedTask;
    }
}