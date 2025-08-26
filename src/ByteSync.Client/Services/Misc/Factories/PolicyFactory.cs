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

    public PolicyFactory(ILogger<PolicyFactory> logger)
    {
        _logger = logger;
    }
    
    private const int MAX_RETRIES = 5; // 2s,4s,8s,16s,32s
    
    private TimeSpan SleepDurationProvider(int retryAttempt)
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
            .Or<HttpRequestException>(e => e.StatusCode == System.Net.HttpStatusCode.Forbidden)
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                _logger.LogError("FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode}" +
                                 "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                                 "Waiting {WaitingTime} seconds before retry", 
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
            .Or<ApiException>(ex => ex.HttptatusCode == HttpStatusCode.Unauthorized 
                                     || ex.HttptatusCode == HttpStatusCode.ServiceUnavailable 
                                     || ex.HttptatusCode == HttpStatusCode.BadGateway
                                     || ex.HttptatusCode == HttpStatusCode.GatewayTimeout
                                     || ex.HttptatusCode == HttpStatusCode.RequestTimeout
                                     || ex.HttptatusCode == HttpStatusCode.InternalServerError)
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                _logger.LogError("FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode}" +
                                 "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                                 "Waiting {WaitingTime} seconds before retry", 
                    retryCount, response.Result?.StatusCode!, response.Exception?.GetType().Name!, response.Exception?.Message!, timeSpan);
                await Task.CompletedTask;
            });
        
        return policy;
    }
}