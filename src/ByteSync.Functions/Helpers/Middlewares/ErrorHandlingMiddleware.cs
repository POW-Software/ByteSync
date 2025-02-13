using System.Net;
using ByteSync.ServerCommon.Exceptions;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Helpers.Middlewares;

public class ErrorHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(TelemetryClient telemetryClient, ILogger<ErrorHandlingMiddleware> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BadRequestException ex)
        {
            _telemetryClient.TrackException(ex);
            _logger.LogWarning(ex, "An error occurred in function {FunctionName}", context.FunctionDefinition.Name);
            
            var httpRequest = await context.GetHttpRequestDataAsync();
            if (httpRequest != null)
            {
                PrepareResponse(context, httpRequest, HttpStatusCode.BadRequest);
                
                return;
            }
            throw;
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
            _logger.LogError(ex, "An error occurred in function {FunctionName}", context.FunctionDefinition.Name);
            
            var httpRequest = await context.GetHttpRequestDataAsync();
            if (httpRequest != null)
            {
                PrepareResponse(context, httpRequest, HttpStatusCode.InternalServerError);

                return;
            }
            throw;
        }
    }

    private static void PrepareResponse(FunctionContext context, HttpRequestData httpRequest, HttpStatusCode statusCode)
    {
        var response = httpRequest.CreateResponse();
        response.StatusCode = statusCode;
        context.GetInvocationResult().Value = response;
    }
}