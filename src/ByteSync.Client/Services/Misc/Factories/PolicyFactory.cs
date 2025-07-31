using Azure;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Interfaces;
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
    
    private const int MAX_RETRIES = 4;

    private TimeSpan SleepDurationProvider(int retryAttempt)
    {
        var seconds = 3;
        if (retryAttempt > 1)
        {
            seconds = 5;
        }
        
        var result = TimeSpan.FromSeconds(seconds);
        
        return result;
    }

    public AsyncRetryPolicy<Response> BuildFileDownloadPolicy()
    {
        var policy = Policy
            .HandleResult<Response>(x => x.IsError)
            .Or<RequestFailedException>(e => e.Status == 403)
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                _logger.LogError("FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode}" +
                                 "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                                 "Waiting {WaitingTime} seconds before retry", 
                    retryCount, response.Result?.Status!, response.Exception?.GetType().Name!, response.Exception?.Message!, timeSpan);
                await Task.CompletedTask;
            });
        
        return policy;
    }

    public AsyncRetryPolicy<UploadFileResponse> BuildFileUploadPolicy()
    {
        var policy = Policy
            .HandleResult<UploadFileResponse>(x => !x.IsSuccess)
            .Or<RequestFailedException>(e => e.Status == 403)
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