using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Helpers;

public class TelemetryAndErrorHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryAndErrorHandlingMiddleware> _logger;

    public TelemetryAndErrorHandlingMiddleware(TelemetryClient telemetryClient, ILogger<TelemetryAndErrorHandlingMiddleware> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>(context.FunctionDefinition.Name);
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["OperationId"] = operation.Telemetry.Context.Operation.Id,
                   ["FunctionName"] = context.FunctionDefinition.Name,
               }))
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                _logger.LogError(ex, "An error occurred in function {FunctionName}", context.FunctionDefinition.Name);
                
                if (context.BindingContext.BindingData.TryGetValue("req", out var reqObj) && reqObj is HttpRequestData req)
                {
                    var response = req.CreateResponse();
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    await response.WriteStringAsync("{ \"error\": \"Internal Server Error\" }");
                    
                    context.GetInvocationResult().Value = response;
                    return;
                }
                
                throw;
            }
        }
    }
}