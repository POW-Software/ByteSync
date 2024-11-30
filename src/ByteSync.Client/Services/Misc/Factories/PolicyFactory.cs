using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using ByteSync.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Polly;
using Polly.Retry;
using RestSharp;
using Serilog;

namespace ByteSync.Services.Misc.Factories;

public class PolicyFactory : IPolicyFactory
{
    private const int MAX_RETRIES = 4;
    
    public AsyncRetryPolicy BuildHubPolicy()
    {
        var policy = Policy
            .Handle<WebSocketException>()
            .Or<InvalidOperationException>(e => e.Message.Contains("The 'InvokeCoreAsync' method cannot be called if the connection is not active"))
            .Or<HubException>(e => !e.Message.Contains("InvalidDataException"))
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (exception, timeSpan, retryCount, _) =>
            {
                Log.Error("HubOperation failed (Attempt number {AttemptNumber}). ExceptionType:{ExceptionType}, " +
                          "ExceptionMessage:{ExceptionMessage}. Waiting {WaitingTime} seconds before retry", 
                    retryCount, exception.GetType().FullName, exception.Message, timeSpan);

                await Task.CompletedTask;
            });

        return policy;
    }

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

    public AsyncRetryPolicy<RestResponse> BuildRestPolicy()
    {
        var retryPolicy = Policy
            .HandleResult<RestResponse>(x => !x.IsSuccessful)
            .Or<WebSocketException>()
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (iRestResponse, timeSpan, retryCount, _) =>
            {
                Log.Error("HubOperation failed (Attempt number {AttemptNumber}). HttpStatusCode:{HttpStatusCode}? " +
                          "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                          "Uri:{Uri}. Waiting {WaitingTime} seconds before retry", 
                    retryCount, iRestResponse.Result.StatusCode, iRestResponse.Result?.ResponseUri?.ToString() ?? "", 
                    iRestResponse.Exception?.GetType().Name!, iRestResponse.Exception?.Message!, timeSpan);

                await Task.CompletedTask;
            });

        return retryPolicy;
    }

    public AsyncRetryPolicy<HttpResponseMessage> BuildHttpPolicy(int? maxAttempts = null)
    {
        int? maxRetries = maxAttempts - 1;
        if (maxRetries == null || maxRetries < 1)
        {
            maxRetries = MAX_RETRIES;
        }
        
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(x => !x.IsSuccessStatusCode 
                && x.StatusCode != HttpStatusCode.Forbidden && x.StatusCode != HttpStatusCode.Unauthorized)
            .Or<HttpRequestException>(e => e.InnerException is SocketException)
            .WaitAndRetryAsync(maxRetries.Value, SleepDurationProvider, onRetryAsync: async (responseMessage, timeSpan, retryCount, _) =>
            {
                Log.Error("HttpOperation failed (Attempt number {AttemptNumber}). HttpStatusCode:{HttpStatusCode}. " +
                          "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                          "Waiting {WaitingTime} seconds before retry", 
                    retryCount, responseMessage.Result?.StatusCode!, responseMessage.Exception?.GetType().Name!, responseMessage.Exception?.Message!, timeSpan);

                await Task.CompletedTask;
            });

        return retryPolicy;
    }

    public AsyncRetryPolicy<Response> BuildFileDownloadPolicy()
    {
        var policy = Policy
            .HandleResult<Response>(x => x.IsError)
            .Or<RequestFailedException>(e => e.Status == 403)
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                Log.Error("FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode}" +
                          "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                          "Waiting {WaitingTime} seconds before retry", 
                    retryCount, response.Result?.Status!, response.Exception?.GetType().Name!, response.Exception?.Message!, timeSpan);
                await Task.CompletedTask;
            });
        
        return policy;
    }

    public AsyncRetryPolicy<Response<BlobContentInfo>> BuildFileUploadPolicy()
    {
        var policy = Policy
            .HandleResult<Response<BlobContentInfo>>(x => x.GetRawResponse().IsError)
            .Or<RequestFailedException>(e => e.Status == 403)
            .WaitAndRetryAsync(MAX_RETRIES, SleepDurationProvider, onRetryAsync: async (response, timeSpan, retryCount, _) =>
            {
                Log.Error("FileTransferOperation failed (Attempt number {AttemptNumber}). ResponseCode:{ResponseCode}" +
                          "ExceptionType:{ExceptionType}, ExceptionMessage:{ExceptionMessage}. " +
                          "Waiting {WaitingTime} seconds before retry", 
                    retryCount, response.Result?.GetRawResponse().Status!, response.Exception?.GetType().Name!, response.Exception?.Message!, timeSpan);
                await Task.CompletedTask;
            });
        
        return policy;
    }
}