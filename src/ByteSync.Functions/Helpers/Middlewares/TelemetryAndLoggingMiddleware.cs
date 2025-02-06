using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Helpers.Middlewares;

public class TelemetryAndLoggingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryAndLoggingMiddleware> _logger;

    public TelemetryAndLoggingMiddleware(TelemetryClient telemetryClient, ILogger<TelemetryAndLoggingMiddleware> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>(context.FunctionDefinition.Name);
        
        var scopeProperties = new Dictionary<string, object>
        {
            ["OperationId"] = operation.Telemetry.Context.Operation.Id,
            ["FunctionName"] = context.FunctionDefinition.Name
        };
        
        if (context.BindingContext.BindingData.TryGetValue("sessionId", out var sessionId))
        {
            scopeProperties["SessionId"] = sessionId!;
        }
        
        if (context.BindingContext.BindingData.TryGetValue("cloudSessionProfileId", out var cloudSessionProfileId))
        {
            scopeProperties["CloudSessionProfileId"] = cloudSessionProfileId!;
        }

        using (_logger.BeginScope(scopeProperties))
        {
            await next(context);
        }
    }
}