using System.Net;
using ByteSync.Functions.Constants;
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
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
            _logger.LogError(ex, "An error occurred in function {FunctionName}", context.FunctionDefinition.Name);
            
            var httpRequest = await context.GetHttpRequestDataAsync();
            if (httpRequest != null)
            {
                var response = httpRequest.CreateResponse();
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);

                context.GetInvocationResult().Value = response;
                return;
            }
            throw;
        }
    }
}