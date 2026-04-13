using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Interfaces;
using ByteSync.Exceptions;
using Polly;
using Polly.Retry;

namespace ByteSync.Services.Misc.Factories;

public class PolicyFactory : IPolicyFactory
{
    private readonly ILogger<PolicyFactory> _logger;
    private readonly Func<int, TimeSpan> _sleepDurationProvider;

    public PolicyFactory(ILogger<PolicyFactory> logger)
        : this(logger, DefaultSleepDurationProvider)
    {
    }

    public PolicyFactory(ILogger<PolicyFactory> logger, Func<int, TimeSpan> sleepDurationProvider)
    {
        _logger = logger;
        _sleepDurationProvider = sleepDurationProvider;
    }
    
    private const int MAX_RETRIES = 5;
    
    private static TimeSpan DefaultSleepDurationProvider(int retryAttempt)
    {
        // Exponential backoff with jitter: 2^({attempt}-1) seconds + 0-500ms
        var baseSeconds = Math.Pow(2, Math.Max(0, retryAttempt - 1));
        var jitterMs = RandomNumberGenerator.GetInt32(0, 500);
        var delay = TimeSpan.FromSeconds(baseSeconds) + TimeSpan.FromMilliseconds(jitterMs);
        // Cap at 45s to avoid excessive waits
        if (delay > TimeSpan.FromSeconds(45))
        {
            delay = TimeSpan.FromSeconds(45);
        }
        return delay;
    }

    public AsyncRetryPolicy<DownloadFileResponse> BuildFileDownloadPolicy()
    {
        var policy = Policy
            .HandleResult<DownloadFileResponse>(x => !x.IsSuccess)
            .Or<HttpRequestException>(e => e.StatusCode == HttpStatusCode.Forbidden)
            .WaitAndRetryAsync(MAX_RETRIES, _sleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                _logger.LogError(response.Exception, 
                    "FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode} ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. Waiting {WaitingTime} seconds before retry",
                    retryCount, response.Result?.StatusCode!, response.Exception?.GetType().Name!, response.Exception?.Message!, timeSpan);
                await Task.CompletedTask;
            });
        
        return policy;
    }

    public AsyncRetryPolicy<UploadFileResponse> BuildFileUploadPolicy()
    {
        var policy = Policy
            .HandleResult<UploadFileResponse>(x => !x.IsSuccess)
            .Or<HttpRequestException>(e => e.StatusCode == HttpStatusCode.Forbidden 
                                           || e.StatusCode == HttpStatusCode.Unauthorized 
                                           || e.StatusCode == HttpStatusCode.ServiceUnavailable 
                                           || e.StatusCode == HttpStatusCode.BadGateway
                                           || e.StatusCode == HttpStatusCode.GatewayTimeout
                                           || e.StatusCode == HttpStatusCode.RequestTimeout
                                           || e.StatusCode == HttpStatusCode.InternalServerError)
            .Or<ApiException>(ex => ex.HttpStatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.ServiceUnavailable
                                    || ex.HttpStatusCode == HttpStatusCode.BadGateway
                                    || ex.HttpStatusCode == HttpStatusCode.GatewayTimeout
                                    || ex.HttpStatusCode == HttpStatusCode.RequestTimeout
                                    || ex.HttpStatusCode == HttpStatusCode.InternalServerError)
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(MAX_RETRIES, _sleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                _logger.LogError(response.Exception,
                    "FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode} ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. Waiting {WaitingTime} seconds before retry",
                    retryCount, response.Result?.StatusCode!, response.Exception?.GetType().Name!, response.Exception?.Message!, timeSpan);
                await Task.CompletedTask;
            });
        
        return policy;
    }
}
